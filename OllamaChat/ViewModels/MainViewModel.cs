using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OllamaChat.Models;
using OllamaChat.Services;

namespace OllamaChat.ViewModels;

/// <summary>
/// Main ViewModel for the chat application
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IChatService _chatService;
    private readonly IOllamaService _ollamaService;
    private readonly FileService _fileService;
    private CancellationTokenSource? _streamCancellation;

    [ObservableProperty]
    private ObservableCollection<ProjectViewModel> _projects = new();

    [ObservableProperty]
    private ObservableCollection<ChatListItemViewModel> _recentChats = new();

    [ObservableProperty]
    private ObservableCollection<ChatMessageViewModel> _messages = new();

    [ObservableProperty]
    private ObservableCollection<ArtifactViewModel> _artifacts = new();

    [ObservableProperty]
    private ObservableCollection<OllamaModel> _availableModels = new();

    [ObservableProperty]
    private ObservableCollection<string> _pendingAttachments = new();

    [ObservableProperty]
    private Chat? _currentChat;

    [ObservableProperty]
    private ProjectViewModel? _selectedProject;

    [ObservableProperty]
    private ChatListItemViewModel? _selectedChatItem;

    [ObservableProperty]
    private string _messageInput = string.Empty;

    [ObservableProperty]
    private string _selectedModel = "llama3.2";

    [ObservableProperty]
    private bool _isServerConnected;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    [ObservableProperty]
    private bool _isArtifactsPanelVisible;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private string _newProjectName = string.Empty;

    [ObservableProperty]
    private bool _useProjectContext;

    [ObservableProperty]
    private bool _isAssignProjectDialogVisible;

    [ObservableProperty]
    private ChatListItemViewModel? _chatToAssign;

    public bool CurrentChatHasProject => CurrentChat?.ProjectId != null;

    public MainViewModel(IChatService chatService, IOllamaService ollamaService, FileService fileService)
    {
        _chatService = chatService;
        _ollamaService = ollamaService;
        _fileService = fileService;
    }

    partial void OnCurrentChatChanged(Chat? value)
    {
        OnPropertyChanged(nameof(CurrentChatHasProject));
    }

    public async Task InitializeAsync()
    {
        await LoadProjectsAsync();
        await LoadRecentChatsAsync();
        await CheckServerConnectionAsync();
        await LoadModelsAsync();
    }

    [RelayCommand]
    private async Task CheckServerConnectionAsync()
    {
        IsServerConnected = await _ollamaService.IsServerAvailableAsync();
        ConnectionStatus = IsServerConnected ? "Connected" : "Disconnected";
    }

    [RelayCommand]
    private async Task LoadModelsAsync()
    {
        if (!IsServerConnected) return;

        var models = await _ollamaService.GetModelsAsync();
        AvailableModels.Clear();
        foreach (var model in models)
        {
            AvailableModels.Add(model);
        }

        if (AvailableModels.Any() && string.IsNullOrEmpty(SelectedModel))
        {
            SelectedModel = AvailableModels.First().Name;
        }
    }

    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        var projects = await _chatService.GetProjectsAsync();
        Projects.Clear();
        foreach (var project in projects)
        {
            Projects.Add(new ProjectViewModel(project));
        }
    }

    [RelayCommand]
    private async Task LoadRecentChatsAsync()
    {
        var chats = await _chatService.GetRecentChatsAsync(50);
        RecentChats.Clear();
        foreach (var chat in chats)
        {
            RecentChats.Add(new ChatListItemViewModel(chat));
        }
    }

    [RelayCommand]
    private async Task CreateNewChatAsync()
    {
        var chat = await _chatService.CreateChatAsync(
            SelectedProject?.Id,
            SelectedModel);

        var chatItem = new ChatListItemViewModel(chat);
        RecentChats.Insert(0, chatItem);
        await SelectChatAsync(chatItem);
    }

    [RelayCommand]
    private async Task SelectChatAsync(ChatListItemViewModel? chatItem)
    {
        if (chatItem == null) return;

        SelectedChatItem = chatItem;
        CurrentChat = await _chatService.GetChatByIdAsync(chatItem.Id);

        Messages.Clear();
        foreach (var message in CurrentChat.Messages)
        {
            Messages.Add(new ChatMessageViewModel(message));
        }

        Artifacts.Clear();
        foreach (var artifact in CurrentChat.Artifacts)
        {
            Artifacts.Add(new ArtifactViewModel(artifact));
        }

        SelectedModel = CurrentChat.ModelName;
        IsArtifactsPanelVisible = Artifacts.Any();
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageInput) || IsSending) return;
        if (CurrentChat == null)
        {
            await CreateNewChatAsync();
        }
        if (CurrentChat == null) return;

        IsSending = true;
        ClearError();
        _streamCancellation = new CancellationTokenSource();

        try
        {
            // Add user message
            var userMessage = await _chatService.AddMessageAsync(
                CurrentChat.Id, "user", MessageInput);

            var userMessageVm = new ChatMessageViewModel(userMessage);

            // Handle file attachments
            foreach (var filePath in PendingAttachments)
            {
                var attachment = await _fileService.UploadFileAsync(userMessage.Id, filePath);
                userMessageVm.Attachments.Add(attachment);
            }
            PendingAttachments.Clear();

            Messages.Add(userMessageVm);

            // Prepare assistant message placeholder
            var assistantMessageVm = new ChatMessageViewModel
            {
                Role = "assistant",
                IsAssistant = true,
                IsStreaming = true,
                CreatedAt = DateTime.UtcNow
            };
            Messages.Add(assistantMessageVm);

            // Build request
            var request = await BuildChatRequestAsync();
            var userInput = MessageInput;
            MessageInput = string.Empty;

            // Stream response
            var fullResponse = new System.Text.StringBuilder();
            var config = _ollamaService.GetConfig();

            if (config.StreamResponses)
            {
                await foreach (var response in _ollamaService.ChatStreamAsync(request, _streamCancellation.Token))
                {
                    if (response.Message?.Content != null)
                    {
                        fullResponse.Append(response.Message.Content);
                        assistantMessageVm.AppendContent(response.Message.Content);
                    }

                    if (response.Done)
                    {
                        break;
                    }
                }
            }
            else
            {
                var response = await _ollamaService.ChatAsync(request, _streamCancellation.Token);
                if (response.Message?.Content != null)
                {
                    fullResponse.Append(response.Message.Content);
                    assistantMessageVm.Content = response.Message.Content;
                }
            }

            assistantMessageVm.IsStreaming = false;

            // Save assistant message
            var savedMessage = await _chatService.AddMessageAsync(
                CurrentChat.Id, "assistant", fullResponse.ToString());
            assistantMessageVm.Id = savedMessage.Id;

            // Extract and save artifacts from the response
            await ExtractAndSaveArtifactsAsync(fullResponse.ToString(), savedMessage.Id);

            // Refresh chat from database (title may have been auto-updated)
            CurrentChat = await _chatService.GetChatByIdAsync(CurrentChat.Id);

            // Update chat item in sidebar
            var chatItem = RecentChats.FirstOrDefault(c => c.Id == CurrentChat.Id);
            if (chatItem != null)
            {
                chatItem.Title = CurrentChat.Title;
                chatItem.LastMessage = fullResponse.ToString().Length > 100
                    ? fullResponse.ToString()[..100] + "..."
                    : fullResponse.ToString();
                chatItem.UpdatedAt = DateTime.UtcNow;

                // Move to top
                RecentChats.Remove(chatItem);
                RecentChats.Insert(0, chatItem);
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
        }
        finally
        {
            IsSending = false;
            _streamCancellation?.Dispose();
            _streamCancellation = null;
        }
    }

    [RelayCommand]
    private void StopGenerating()
    {
        _streamCancellation?.Cancel();
    }

    [RelayCommand]
    private void AttachFile()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "All Files (*.*)|*.*|Images (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|Documents (*.txt;*.md;*.pdf)|*.txt;*.md;*.pdf|Code Files (*.cs;*.js;*.py;*.ts)|*.cs;*.js;*.py;*.ts"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                if (!PendingAttachments.Contains(file))
                {
                    PendingAttachments.Add(file);
                }
            }
        }
    }

    [RelayCommand]
    private void RemoveAttachment(string filePath)
    {
        PendingAttachments.Remove(filePath);
    }

    [RelayCommand]
    private async Task CreateProjectAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var project = await _chatService.CreateProjectAsync(name, SelectedProject?.Id);
        var projectVm = new ProjectViewModel(project);

        if (SelectedProject != null)
        {
            SelectedProject.Children.Add(projectVm);
        }
        else
        {
            Projects.Add(projectVm);
        }
    }

    [RelayCommand]
    private async Task CreateProjectFromInputAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProjectName)) return;

        var project = await _chatService.CreateProjectAsync(NewProjectName.Trim());
        var projectVm = new ProjectViewModel(project);
        Projects.Add(projectVm);
        NewProjectName = string.Empty;
    }

    [RelayCommand]
    private async Task SelectProjectAsync(ProjectViewModel? project)
    {
        // Deselect previous
        if (SelectedProject != null)
        {
            SelectedProject.IsSelected = false;
        }

        SelectedProject = project;

        if (project != null)
        {
            project.IsSelected = true;
            // Filter chats by project
            var chats = await _chatService.GetChatsAsync(project.Id);
            RecentChats.Clear();
            foreach (var chat in chats)
            {
                RecentChats.Add(new ChatListItemViewModel(chat));
            }
        }
        else
        {
            // Show all recent chats
            await LoadRecentChatsAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteProjectAsync(ProjectViewModel? project)
    {
        if (project == null) return;

        await _chatService.DeleteProjectAsync(project.Id);
        Projects.Remove(project);

        if (SelectedProject?.Id == project.Id)
        {
            SelectedProject = null;
            await LoadRecentChatsAsync();
        }
    }

    [RelayCommand]
    private async Task ClearProjectFilterAsync()
    {
        if (SelectedProject != null)
        {
            SelectedProject.IsSelected = false;
        }
        SelectedProject = null;
        await LoadRecentChatsAsync();
    }

    [RelayCommand]
    private void ShowAssignToProjectDialog(ChatListItemViewModel? chatItem)
    {
        if (chatItem == null) return;
        ChatToAssign = chatItem;
        IsAssignProjectDialogVisible = true;
    }

    [RelayCommand]
    private async Task AssignChatToProjectAsync(ProjectViewModel? project)
    {
        if (ChatToAssign == null || project == null) return;

        await _chatService.MoveChatToProjectAsync(ChatToAssign.Id, project.Id);
        ChatToAssign.ProjectId = project.Id;
        ChatToAssign.ProjectName = project.Name;

        // Refresh project chat counts
        await LoadProjectsAsync();

        IsAssignProjectDialogVisible = false;
        ChatToAssign = null;
    }

    [RelayCommand]
    private async Task RemoveChatFromProjectAsync(ChatListItemViewModel? chatItem)
    {
        if (chatItem == null) return;

        await _chatService.MoveChatToProjectAsync(chatItem.Id, null);
        chatItem.ProjectId = null;
        chatItem.ProjectName = null;

        // Refresh project chat counts
        await LoadProjectsAsync();

        // If filtering by a project, remove this chat from view
        if (SelectedProject != null)
        {
            RecentChats.Remove(chatItem);
        }
    }

    [RelayCommand]
    private void CancelAssignProject()
    {
        IsAssignProjectDialogVisible = false;
        ChatToAssign = null;
    }

    [RelayCommand]
    private async Task DeleteChatAsync(ChatListItemViewModel? chatItem)
    {
        if (chatItem == null) return;

        await _chatService.DeleteChatAsync(chatItem.Id);
        RecentChats.Remove(chatItem);

        if (CurrentChat?.Id == chatItem.Id)
        {
            CurrentChat = null;
            Messages.Clear();
            Artifacts.Clear();
        }
    }

    [RelayCommand]
    private void StartRenameChat(ChatListItemViewModel? chatItem)
    {
        if (chatItem == null) return;

        // Cancel any other editing
        foreach (var chat in RecentChats)
        {
            chat.IsEditing = false;
        }

        chatItem.EditingTitle = chatItem.Title;
        chatItem.IsEditing = true;
    }

    [RelayCommand]
    private async Task ConfirmRenameChatAsync(ChatListItemViewModel? chatItem)
    {
        if (chatItem == null || !chatItem.IsEditing) return;

        var newTitle = chatItem.EditingTitle?.Trim();
        if (!string.IsNullOrEmpty(newTitle) && newTitle != chatItem.Title)
        {
            var chat = await _chatService.GetChatByIdAsync(chatItem.Id);
            chat.Title = newTitle;
            await _chatService.UpdateChatAsync(chat);
            chatItem.Title = newTitle;

            // Update current chat if it's the same
            if (CurrentChat?.Id == chatItem.Id)
            {
                CurrentChat.Title = newTitle;
            }
        }

        chatItem.IsEditing = false;
    }

    [RelayCommand]
    private void CancelRenameChat(ChatListItemViewModel? chatItem)
    {
        if (chatItem == null) return;
        chatItem.IsEditing = false;
    }

    [RelayCommand]
    private async Task SearchChatsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadRecentChatsAsync();
            return;
        }

        var results = await _chatService.SearchChatsAsync(SearchQuery);
        RecentChats.Clear();
        foreach (var chat in results)
        {
            RecentChats.Add(new ChatListItemViewModel(chat));
        }
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
    }

    [RelayCommand]
    private void ToggleArtifactsPanel()
    {
        IsArtifactsPanelVisible = !IsArtifactsPanelVisible;
    }

    [RelayCommand]
    private async Task SaveArtifactAsync(ArtifactViewModel? artifactVm)
    {
        if (artifactVm == null) return;

        var dialog = new SaveFileDialog
        {
            FileName = artifactVm.Title,
            Filter = GetFilterForArtifact(artifactVm)
        };

        if (dialog.ShowDialog() == true)
        {
            var artifact = await _chatService.GetArtifactsAsync(CurrentChat!.Id);
            var targetArtifact = artifact.FirstOrDefault(a => a.Id == artifactVm.Id);
            if (targetArtifact != null)
            {
                await _fileService.SaveArtifactAsync(targetArtifact, dialog.FileName);
            }
        }
    }

    [RelayCommand]
    private void CopyArtifact(ArtifactViewModel? artifactVm)
    {
        if (artifactVm == null) return;
        System.Windows.Clipboard.SetText(artifactVm.Content);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var config = _ollamaService.GetConfig();
        var settingsWindow = new Views.SettingsWindow(config)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (settingsWindow.ShowDialog() == true)
        {
            _ollamaService.UpdateConfig(config);
            _ = CheckServerConnectionAsync();
            _ = LoadModelsAsync();
        }
    }

    private async Task<OllamaChatRequest> BuildChatRequestAsync()
    {
        var messages = new List<OllamaChatMessage>();

        // Add system prompt if exists
        if (!string.IsNullOrEmpty(CurrentChat?.SystemPrompt))
        {
            messages.Add(new OllamaChatMessage
            {
                Role = "system",
                Content = CurrentChat.SystemPrompt
            });
        }

        // Add project context if enabled and chat belongs to a project
        if (UseProjectContext && CurrentChat?.ProjectId != null)
        {
            var projectContext = await GetProjectContextAsync(CurrentChat.ProjectId.Value, CurrentChat.Id);
            if (!string.IsNullOrEmpty(projectContext))
            {
                messages.Add(new OllamaChatMessage
                {
                    Role = "system",
                    Content = $"[Project Context - Previous conversations in this project:]\n{projectContext}"
                });
            }
        }

        // Add conversation history
        foreach (var msg in Messages.Where(m => !m.IsStreaming))
        {
            var ollamaMsg = new OllamaChatMessage
            {
                Role = msg.Role,
                Content = msg.Content
            };

            // Add images for vision models
            var images = msg.Attachments
                .Where(a => a.IsImage && !string.IsNullOrEmpty(a.Base64Content))
                .Select(a => a.Base64Content!)
                .ToArray();

            if (images.Any())
            {
                ollamaMsg.Images = images;
            }

            messages.Add(ollamaMsg);
        }

        return new OllamaChatRequest
        {
            Model = SelectedModel,
            Messages = messages,
            Stream = _ollamaService.GetConfig().StreamResponses,
            Options = _ollamaService.GetConfig().DefaultOptions
        };
    }

    private async Task<string> GetProjectContextAsync(int projectId, int excludeChatId)
    {
        var projectChats = await _chatService.GetChatsAsync(projectId);
        var contextBuilder = new System.Text.StringBuilder();

        foreach (var chat in projectChats.Where(c => c.Id != excludeChatId).Take(5))
        {
            var fullChat = await _chatService.GetChatByIdAsync(chat.Id);
            if (fullChat.Messages.Any())
            {
                contextBuilder.AppendLine($"--- Chat: {fullChat.Title} ---");
                foreach (var msg in fullChat.Messages.TakeLast(10))
                {
                    contextBuilder.AppendLine($"{msg.Role}: {msg.Content}");
                }
                contextBuilder.AppendLine();
            }
        }

        return contextBuilder.ToString();
    }

    private async Task ExtractAndSaveArtifactsAsync(string content, int messageId)
    {
        // Simple artifact extraction for code blocks
        var codeBlockPattern = @"```(\w+)?\s*\n([\s\S]*?)```";
        var matches = System.Text.RegularExpressions.Regex.Matches(content, codeBlockPattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var language = match.Groups[1].Value;
            var code = match.Groups[2].Value.Trim();

            if (string.IsNullOrWhiteSpace(code)) continue;

            var artifact = await _chatService.CreateArtifactAsync(
                CurrentChat!.Id,
                $"Code ({language})",
                code,
                "code",
                language,
                messageId);

            Artifacts.Add(new ArtifactViewModel(artifact));
        }

        if (Artifacts.Any())
        {
            IsArtifactsPanelVisible = true;
        }
    }

    private static string GetFilterForArtifact(ArtifactViewModel artifact)
    {
        return artifact.ArtifactType.ToLowerInvariant() switch
        {
            "code" => artifact.Language?.ToLowerInvariant() switch
            {
                "csharp" or "c#" => "C# Files (*.cs)|*.cs|All Files (*.*)|*.*",
                "javascript" or "js" => "JavaScript Files (*.js)|*.js|All Files (*.*)|*.*",
                "typescript" or "ts" => "TypeScript Files (*.ts)|*.ts|All Files (*.*)|*.*",
                "python" or "py" => "Python Files (*.py)|*.py|All Files (*.*)|*.*",
                "html" => "HTML Files (*.html)|*.html|All Files (*.*)|*.*",
                "css" => "CSS Files (*.css)|*.css|All Files (*.*)|*.*",
                "json" => "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                _ => "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            },
            "markdown" => "Markdown Files (*.md)|*.md|All Files (*.*)|*.*",
            "html" => "HTML Files (*.html)|*.html|All Files (*.*)|*.*",
            _ => "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
        };
    }
}
