using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private string _thinkingContent = string.Empty;

    [ObservableProperty]
    private bool _hasThinkingContent;

    [ObservableProperty]
    private bool _isShowingThinking;

    [ObservableProperty]
    private bool _isCurrentlyThinking;

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
        CreatedAt = message.CreatedAt;
        IsUser = message.Role == "user";
        IsAssistant = message.Role == "assistant";
        Attachments = message.Attachments?.ToList() ?? new List<FileAttachment>();

        // Parse thinking content from stored message
        ParseContent(message.Content);
    }

    private void ParseContent(string content)
    {
        var thinkPattern = @"<think>([\s\S]*?)</think>";
        var matches = System.Text.RegularExpressions.Regex.Matches(content, thinkPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (matches.Count > 0)
        {
            // Extract all thinking content
            var thinkingBuilder = new System.Text.StringBuilder();
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                thinkingBuilder.Append(match.Groups[1].Value);
            }
            ThinkingContent = thinkingBuilder.ToString();
            HasThinkingContent = true;

            // Remove think tags from visible content
            var visibleContent = System.Text.RegularExpressions.Regex.Replace(content, thinkPattern, "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Content = visibleContent.Trim();
        }
        else
        {
            Content = content;
        }
    }

    public void AppendContent(string text)
    {
        Content += text;
        OnPropertyChanged(nameof(Content));
    }

    public void AppendThinkingContent(string text)
    {
        ThinkingContent += text;
        HasThinkingContent = true;
        OnPropertyChanged(nameof(ThinkingContent));
    }

    [RelayCommand]
    private void ToggleThinking()
    {
        IsShowingThinking = !IsShowingThinking;
    }
}
