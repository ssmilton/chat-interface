using CommunityToolkit.Mvvm.ComponentModel;
using OllamaChat.Models;

namespace OllamaChat.ViewModels;

/// <summary>
/// ViewModel for displaying a chat in the sidebar list
/// </summary>
public partial class ChatListItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _title = "New Chat";

    [ObservableProperty]
    private string _lastMessage = string.Empty;

    [ObservableProperty]
    private DateTime _updatedAt;

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _modelName = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editingTitle = string.Empty;

    [ObservableProperty]
    private int? _projectId;

    [ObservableProperty]
    private string? _projectName;

    public ChatListItemViewModel() { }

    public ChatListItemViewModel(Chat chat)
    {
        Id = chat.Id;
        Title = chat.Title;
        UpdatedAt = chat.UpdatedAt;
        IsPinned = chat.IsPinned;
        ModelName = chat.ModelName;
        ProjectId = chat.ProjectId;
        ProjectName = chat.Project?.Name;

        var lastMsg = chat.Messages?.LastOrDefault();
        if (lastMsg != null)
        {
            LastMessage = lastMsg.Content.Length > 100
                ? lastMsg.Content[..100] + "..."
                : lastMsg.Content;
        }
    }

    public string FormattedDate => FormatDate(UpdatedAt);

    private static string FormatDate(DateTime date)
    {
        var now = DateTime.UtcNow;
        var diff = now - date;

        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";

        return date.ToString("MMM d");
    }
}
