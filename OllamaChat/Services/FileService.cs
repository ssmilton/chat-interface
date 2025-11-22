using OllamaChat.Data;
using OllamaChat.Models;
using System.IO;

namespace OllamaChat.Services;

/// <summary>
/// Service for handling file uploads and attachments
/// </summary>
public class FileService
{
    private readonly ChatDbContext _context;
    private readonly IDocumentProcessingService _documentProcessingService;
    private readonly string _uploadsPath;
    private readonly string _artifactsPath;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
    };

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".txt", "text/plain" },
        { ".md", "text/markdown" },
        { ".json", "application/json" },
        { ".xml", "application/xml" },
        { ".html", "text/html" },
        { ".css", "text/css" },
        { ".js", "application/javascript" },
        { ".ts", "application/typescript" },
        { ".cs", "text/x-csharp" },
        { ".py", "text/x-python" },
        { ".java", "text/x-java" },
        { ".cpp", "text/x-c++src" },
        { ".c", "text/x-csrc" },
        { ".h", "text/x-chdr" },
        { ".sql", "application/sql" },
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".webp", "image/webp" },
        { ".svg", "image/svg+xml" },
        { ".zip", "application/zip" },
        { ".rar", "application/x-rar-compressed" },
        { ".7z", "application/x-7z-compressed" }
    };

    public FileService(ChatDbContext context, IDocumentProcessingService documentProcessingService)
    {
        _context = context;
        _documentProcessingService = documentProcessingService;

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OllamaChat");

        _uploadsPath = Path.Combine(appDataPath, "Uploads");
        _artifactsPath = Path.Combine(appDataPath, "Artifacts");

        Directory.CreateDirectory(_uploadsPath);
        Directory.CreateDirectory(_artifactsPath);
    }

    /// <summary>
    /// Upload a file and create an attachment record
    /// </summary>
    public async Task<FileAttachment> UploadFileAsync(int messageId, string sourcePath)
    {
        var fileName = Path.GetFileName(sourcePath);
        var extension = Path.GetExtension(sourcePath);
        var isImage = ImageExtensions.Contains(extension);
        var isDocument = _documentProcessingService.CanProcess(sourcePath);
        var contentType = GetContentType(extension);

        // Create unique file name
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var destPath = Path.Combine(_uploadsPath, uniqueName);

        // Copy file
        File.Copy(sourcePath, destPath, true);

        var fileInfo = new FileInfo(destPath);
        string? base64Content = null;

        // For images, store base64 for Ollama vision models
        if (isImage && fileInfo.Length < 10 * 1024 * 1024) // < 10MB
        {
            var bytes = await File.ReadAllBytesAsync(destPath);
            base64Content = Convert.ToBase64String(bytes);
        }

        var attachment = new FileAttachment
        {
            MessageId = messageId,
            FileName = fileName,
            FilePath = destPath,
            ContentType = contentType,
            FileSize = fileInfo.Length,
            IsImage = isImage,
            IsDocument = isDocument,
            Base64Content = base64Content,
            UploadedAt = DateTime.UtcNow
        };

        // Process document if applicable
        if (isDocument)
        {
            await ProcessDocumentAsync(attachment, destPath);
        }

        _context.FileAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        return attachment;
    }

    /// <summary>
    /// Upload a file from a byte array
    /// </summary>
    public async Task<FileAttachment> UploadFileAsync(int messageId, string fileName, byte[] content)
    {
        var extension = Path.GetExtension(fileName);
        var isImage = ImageExtensions.Contains(extension);
        var isDocument = _documentProcessingService.CanProcess(fileName);
        var contentType = GetContentType(extension);

        // Create unique file name
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var destPath = Path.Combine(_uploadsPath, uniqueName);

        // Write file
        await File.WriteAllBytesAsync(destPath, content);

        string? base64Content = null;
        if (isImage && content.Length < 10 * 1024 * 1024)
        {
            base64Content = Convert.ToBase64String(content);
        }

        var attachment = new FileAttachment
        {
            MessageId = messageId,
            FileName = fileName,
            FilePath = destPath,
            ContentType = contentType,
            FileSize = content.Length,
            IsImage = isImage,
            IsDocument = isDocument,
            Base64Content = base64Content,
            UploadedAt = DateTime.UtcNow
        };

        // Process document if applicable
        if (isDocument)
        {
            await ProcessDocumentFromBytesAsync(attachment, content, fileName);
        }

        _context.FileAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        return attachment;
    }

    /// <summary>
    /// Save an artifact to disk
    /// </summary>
    public async Task<string> SaveArtifactAsync(Artifact artifact, string? customPath = null)
    {
        var extension = GetExtensionForArtifact(artifact);
        var fileName = SanitizeFileName(artifact.Title) + extension;

        string filePath;
        if (!string.IsNullOrEmpty(customPath))
        {
            filePath = customPath;
        }
        else
        {
            var chatFolder = Path.Combine(_artifactsPath, $"Chat_{artifact.ChatId}");
            Directory.CreateDirectory(chatFolder);
            filePath = Path.Combine(chatFolder, fileName);
        }

        await File.WriteAllTextAsync(filePath, artifact.Content);

        artifact.FilePath = filePath;
        _context.Artifacts.Update(artifact);
        await _context.SaveChangesAsync();

        return filePath;
    }

    /// <summary>
    /// Read file content as text
    /// </summary>
    public async Task<string> ReadFileContentAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        return await File.ReadAllTextAsync(filePath);
    }

    /// <summary>
    /// Delete an uploaded file
    /// </summary>
    public async Task DeleteFileAsync(int attachmentId)
    {
        var attachment = await _context.FileAttachments.FindAsync(attachmentId);
        if (attachment != null)
        {
            if (File.Exists(attachment.FilePath))
            {
                File.Delete(attachment.FilePath);
            }

            _context.FileAttachments.Remove(attachment);
            await _context.SaveChangesAsync();
        }
    }

    private static string GetContentType(string extension)
    {
        return ContentTypes.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    private static string GetExtensionForArtifact(Artifact artifact)
    {
        return artifact.ArtifactType.ToLowerInvariant() switch
        {
            "code" => artifact.Language?.ToLowerInvariant() switch
            {
                "csharp" or "c#" => ".cs",
                "javascript" or "js" => ".js",
                "typescript" or "ts" => ".ts",
                "python" or "py" => ".py",
                "java" => ".java",
                "html" => ".html",
                "css" => ".css",
                "json" => ".json",
                "xml" => ".xml",
                "sql" => ".sql",
                "bash" or "shell" => ".sh",
                "powershell" or "ps1" => ".ps1",
                "cpp" or "c++" => ".cpp",
                "c" => ".c",
                "go" => ".go",
                "rust" => ".rs",
                "ruby" => ".rb",
                "php" => ".php",
                _ => ".txt"
            },
            "markdown" or "md" => ".md",
            "html" => ".html",
            "json" => ".json",
            "xml" => ".xml",
            _ => ".txt"
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }

    /// <summary>
    /// Process a document file and extract text
    /// </summary>
    private async Task ProcessDocumentAsync(FileAttachment attachment, string filePath)
    {
        try
        {
            var result = await _documentProcessingService.ExtractTextAsync(filePath);
            ApplyProcessingResult(attachment, result);
        }
        catch (Exception ex)
        {
            attachment.ExtractionSuccessful = false;
            attachment.ExtractionError = $"Processing failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Process a document from byte array and extract text
    /// </summary>
    private async Task ProcessDocumentFromBytesAsync(FileAttachment attachment, byte[] content, string fileName)
    {
        try
        {
            var result = await _documentProcessingService.ExtractTextAsync(content, fileName);
            ApplyProcessingResult(attachment, result);
        }
        catch (Exception ex)
        {
            attachment.ExtractionSuccessful = false;
            attachment.ExtractionError = $"Processing failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Apply document processing result to attachment
    /// </summary>
    private static void ApplyProcessingResult(FileAttachment attachment, DocumentProcessingResult result)
    {
        attachment.ExtractionSuccessful = result.IsSuccess;
        attachment.DocumentType = result.DocumentType.ToString();

        if (result.IsSuccess)
        {
            attachment.ExtractedText = result.ExtractedText;
            attachment.PageCount = result.PageCount;
            attachment.SheetCount = result.SheetCount;
            attachment.WordCount = result.WordCount;
            attachment.CharacterCount = result.CharacterCount;
        }
        else
        {
            attachment.ExtractionError = result.ErrorMessage;
        }
    }

    /// <summary>
    /// Get supported document extensions for file dialog filters
    /// </summary>
    public IReadOnlyCollection<string> GetSupportedDocumentExtensions()
    {
        return _documentProcessingService.GetSupportedExtensions();
    }
}
