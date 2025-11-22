using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OllamaChat.Models;

namespace OllamaChat.ViewModels;

/// <summary>
/// ViewModel for displaying a project in the sidebar
/// </summary>
public partial class ProjectViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private ObservableCollection<ProjectViewModel> _children = new();

    [ObservableProperty]
    private ObservableCollection<ChatListItemViewModel> _chats = new();

    public ProjectViewModel() { }

    public ProjectViewModel(Project project)
    {
        Id = project.Id;
        Name = project.Name;
        Description = project.Description;
        IsExpanded = project.IsExpanded;

        foreach (var child in project.ChildProjects)
        {
            Children.Add(new ProjectViewModel(child));
        }

        foreach (var chat in project.Chats)
        {
            Chats.Add(new ChatListItemViewModel(chat));
        }
    }
}
