using CommunityToolkit.Mvvm.ComponentModel;
using OllamaChat.Models;

namespace OllamaChat.ViewModels;

/// <summary>
/// ViewModel for displaying an artifact
/// </summary>
public partial class ArtifactViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _title = "Untitled";

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _artifactType = "text";

    [ObservableProperty]
    private string? _language;

    [ObservableProperty]
    private int _version = 1;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private bool _isExpanded;

    public ArtifactViewModel() { }

    public ArtifactViewModel(Artifact artifact)
    {
        Id = artifact.Id;
        Title = artifact.Title;
        Content = artifact.Content;
        ArtifactType = artifact.ArtifactType;
        Language = artifact.Language;
        Version = artifact.Version;
        CreatedAt = artifact.CreatedAt;
    }

    public string DisplayTitle => string.IsNullOrEmpty(Language)
        ? Title
        : $"{Title} ({Language})";

    public string Icon => ArtifactType.ToLowerInvariant() switch
    {
        "code" => "📄",
        "markdown" => "📝",
        "html" => "🌐",
        "json" => "{ }",
        _ => "📋"
    };
}
