using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OllamaChat.Models;

/// <summary>
/// Represents a file attached to a chat message
/// </summary>
public class FileAttachment
{
    [Key]
    public int Id { get; set; }

    public int MessageId { get; set; }

    [ForeignKey(nameof(MessageId))]
    public ChatMessage Message { get; set; } = null!;

    [Required]
    [MaxLength(300)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // For images that can be sent to vision models
    public bool IsImage { get; set; }

    // Base64 content for small files or images
    public string? Base64Content { get; set; }

    // Document processing fields
    /// <summary>
    /// Whether this file is a processable document (PDF, Office, text, etc.)
    /// </summary>
    public bool IsDocument { get; set; }

    /// <summary>
    /// The extracted text content from the document
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Whether text extraction was successful
    /// </summary>
    public bool ExtractionSuccessful { get; set; }

    /// <summary>
    /// Error message if extraction failed
    /// </summary>
    [MaxLength(500)]
    public string? ExtractionError { get; set; }

    /// <summary>
    /// The type of document (PDF, DOCX, TXT, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? DocumentType { get; set; }

    /// <summary>
    /// Number of pages (for paginated documents like PDF)
    /// </summary>
    public int? PageCount { get; set; }

    /// <summary>
    /// Number of sheets (for spreadsheets)
    /// </summary>
    public int? SheetCount { get; set; }

    /// <summary>
    /// Word count of extracted text
    /// </summary>
    public int? WordCount { get; set; }

    /// <summary>
    /// Character count of extracted text
    /// </summary>
    public int? CharacterCount { get; set; }
}
