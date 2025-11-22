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
}
