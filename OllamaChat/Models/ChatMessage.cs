using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OllamaChat.Models;

/// <summary>
/// Represents a single message in a chat conversation
/// </summary>
public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    public int ChatId { get; set; }

    [ForeignKey(nameof(ChatId))]
    public Chat Chat { get; set; } = null!;

    [Required]
    public string Role { get; set; } = "user"; // "user", "assistant", "system"

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Token usage tracking
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }

    // For streaming responses
    public bool IsComplete { get; set; } = true;

    // Duration in milliseconds
    public long? ResponseDuration { get; set; }

    // Navigation properties
    public ICollection<FileAttachment> Attachments { get; set; } = new List<FileAttachment>();
}
