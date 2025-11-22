namespace OllamaChat.Services;

/// <summary>
/// Result of document processing containing extracted text and metadata
/// </summary>
public class DocumentProcessingResult
{
    /// <summary>
    /// The extracted text content from the document
    /// </summary>
    public string ExtractedText { get; set; } = string.Empty;

    /// <summary>
    /// Whether the extraction was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if extraction failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The type of document that was processed
    /// </summary>
    public DocumentType DocumentType { get; set; }

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
    public int WordCount { get; set; }

    /// <summary>
    /// Character count of extracted text
    /// </summary>
    public int CharacterCount { get; set; }
}

/// <summary>
/// Types of documents that can be processed
/// </summary>
public enum DocumentType
{
    Unknown,
    PlainText,
    Markdown,
    Json,
    Xml,
    Html,
    Csv,
    Pdf,
    WordDocument,
    WordDocumentLegacy,
    ExcelSpreadsheet,
    ExcelSpreadsheetLegacy,
    PowerPointPresentation,
    PowerPointPresentationLegacy,
    RichText,
    SourceCode
}

/// <summary>
/// Service interface for processing and extracting text from various document formats
/// </summary>
public interface IDocumentProcessingService
{
    /// <summary>
    /// Check if a file can be processed based on its extension
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if the file type is supported</returns>
    bool CanProcess(string filePath);

    /// <summary>
    /// Get the document type from a file path
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>The determined document type</returns>
    DocumentType GetDocumentType(string filePath);

    /// <summary>
    /// Extract text content from a document file
    /// </summary>
    /// <param name="filePath">Path to the file to process</param>
    /// <returns>Processing result with extracted text</returns>
    Task<DocumentProcessingResult> ExtractTextAsync(string filePath);

    /// <summary>
    /// Extract text content from a byte array
    /// </summary>
    /// <param name="content">File content as bytes</param>
    /// <param name="fileName">Original file name for type detection</param>
    /// <returns>Processing result with extracted text</returns>
    Task<DocumentProcessingResult> ExtractTextAsync(byte[] content, string fileName);

    /// <summary>
    /// Get supported file extensions
    /// </summary>
    /// <returns>Collection of supported extensions</returns>
    IReadOnlyCollection<string> GetSupportedExtensions();
}
