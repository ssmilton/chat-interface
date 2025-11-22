using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OllamaChat.Models;

/// <summary>
/// Represents a conversation/chat session
/// </summary>
public class Chat
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = "New Chat";

    public int? ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    [MaxLength(100)]
    public string ModelName { get; set; } = "llama3.2";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsArchived { get; set; }

    public bool IsPinned { get; set; }

    // System prompt for this chat
    [MaxLength(5000)]
    public string? SystemPrompt { get; set; }

    // Navigation properties
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public ICollection<Artifact> Artifacts { get; set; } = new List<Artifact>();
}
