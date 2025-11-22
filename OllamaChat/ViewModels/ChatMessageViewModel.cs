using CommunityToolkit.Mvvm.ComponentModel;
using OllamaChat.Models;

namespace OllamaChat.ViewModels;

/// <summary>
/// ViewModel for displaying a chat message
/// </summary>
public partial class ChatMessageViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _role = "user";

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private bool _isUser;

    [ObservableProperty]
    private bool _isAssistant;

    [ObservableProperty]
    private List<FileAttachment> _attachments = new();

    public ChatMessageViewModel() { }

    public ChatMessageViewModel(ChatMessage message)
    {
        Id = message.Id;
        Role = message.Role;
        Content = message.Content;
        CreatedAt = message.CreatedAt;
        IsUser = message.Role == "user";
        IsAssistant = message.Role == "assistant";
        Attachments = message.Attachments?.ToList() ?? new List<FileAttachment>();
    }

    public void AppendContent(string text)
    {
        Content += text;
        OnPropertyChanged(nameof(Content));
    }
}
