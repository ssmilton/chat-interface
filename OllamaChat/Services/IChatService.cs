using OllamaChat.Models;

namespace OllamaChat.Services;

/// <summary>
/// Interface for chat data operations
/// </summary>
public interface IChatService
{
    // Project operations
    Task<List<Project>> GetProjectsAsync();
    Task<Project> CreateProjectAsync(string name, int? parentId = null);
    Task<Project> UpdateProjectAsync(Project project);
    Task DeleteProjectAsync(int projectId);

    // Chat operations
    Task<List<Chat>> GetChatsAsync(int? projectId = null);
    Task<List<Chat>> GetRecentChatsAsync(int count = 20);
    Task<Chat> GetChatByIdAsync(int chatId);
    Task<Chat> CreateChatAsync(int? projectId = null, string? modelName = null);
    Task<Chat> UpdateChatAsync(Chat chat);
    Task DeleteChatAsync(int chatId);
    Task MoveChatToProjectAsync(int chatId, int? projectId);

    // Message operations
    Task<List<ChatMessage>> GetMessagesAsync(int chatId);
    Task<ChatMessage> AddMessageAsync(int chatId, string role, string content);
    Task<ChatMessage> UpdateMessageAsync(ChatMessage message);
    Task DeleteMessageAsync(int messageId);

    // Artifact operations
    Task<List<Artifact>> GetArtifactsAsync(int chatId);
    Task<Artifact> CreateArtifactAsync(int chatId, string title, string content, string type, string? language = null, int? messageId = null);
    Task<Artifact> UpdateArtifactAsync(Artifact artifact);
    Task DeleteArtifactAsync(int artifactId);
    Task<Artifact> CreateArtifactVersionAsync(int parentArtifactId, string content);

    // Search
    Task<List<Chat>> SearchChatsAsync(string query);
}
