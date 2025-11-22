using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OllamaChat.Models;

/// <summary>
/// Represents a generated artifact (code, document, etc.) from a chat
/// </summary>
public class Artifact
{
    [Key]
    public int Id { get; set; }

    public int ChatId { get; set; }

    [ForeignKey(nameof(ChatId))]
    public Chat Chat { get; set; } = null!;

    public int? MessageId { get; set; }

    [ForeignKey(nameof(MessageId))]
    public ChatMessage? Message { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "Untitled Artifact";

    [Required]
    public string Content { get; set; } = string.Empty;

    // Type of artifact: "code", "markdown", "html", "json", "text", etc.
    [MaxLength(50)]
    public string ArtifactType { get; set; } = "text";

    // Programming language for code artifacts
    [MaxLength(50)]
    public string? Language { get; set; }

    // Version tracking
    public int Version { get; set; } = 1;

    public int? ParentArtifactId { get; set; }

    [ForeignKey(nameof(ParentArtifactId))]
    public Artifact? ParentArtifact { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // File path if saved to disk
    [MaxLength(500)]
    public string? FilePath { get; set; }

    public ICollection<Artifact> ChildArtifacts { get; set; } = new List<Artifact>();
}
