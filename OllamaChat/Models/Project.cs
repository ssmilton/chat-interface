using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OllamaChat.Models;

/// <summary>
/// Represents a project/folder for organizing chats
/// </summary>
public class Project
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? ParentProjectId { get; set; }

    [ForeignKey(nameof(ParentProjectId))]
    public Project? ParentProject { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int SortOrder { get; set; }

    public bool IsExpanded { get; set; } = true;

    // Navigation properties
    public ICollection<Project> ChildProjects { get; set; } = new List<Project>();
    public ICollection<Chat> Chats { get; set; } = new List<Chat>();
}
