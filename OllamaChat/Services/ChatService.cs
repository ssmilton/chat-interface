using Microsoft.EntityFrameworkCore;
using OllamaChat.Data;
using OllamaChat.Models;

namespace OllamaChat.Services;

/// <summary>
/// Service for managing chat data operations
/// </summary>
public class ChatService : IChatService
{
    private readonly ChatDbContext _context;

    public ChatService(ChatDbContext context)
    {
        _context = context;
    }

    #region Project Operations

    public async Task<List<Project>> GetProjectsAsync()
    {
        return await _context.Projects
            .Include(p => p.ChildProjects)
            .Include(p => p.Chats)
            .Where(p => p.ParentProjectId == null)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Project> CreateProjectAsync(string name, int? parentId = null)
    {
        var project = new Project
        {
            Name = name,
            ParentProjectId = parentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<Project> UpdateProjectAsync(Project project)
    {
        project.UpdatedAt = DateTime.UtcNow;
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task DeleteProjectAsync(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Chats)
            .Include(p => p.ChildProjects)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project != null)
        {
            // Move chats to no project
            foreach (var chat in project.Chats)
            {
                chat.ProjectId = null;
            }

            // Move child projects to parent
            foreach (var child in project.ChildProjects)
            {
                child.ParentProjectId = project.ParentProjectId;
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Chat Operations

    public async Task<List<Chat>> GetChatsAsync(int? projectId = null)
    {
        var query = _context.Chats
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt).Take(1))
            .Where(c => !c.IsArchived);

        if (projectId.HasValue)
        {
            query = query.Where(c => c.ProjectId == projectId);
        }

        return await query
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<Chat>> GetRecentChatsAsync(int count = 20)
    {
        return await _context.Chats
            .Where(c => !c.IsArchived)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Chat> GetChatByIdAsync(int chatId)
    {
        return await _context.Chats
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .ThenInclude(m => m.Attachments)
            .Include(c => c.Artifacts)
            .Include(c => c.Project)
            .FirstOrDefaultAsync(c => c.Id == chatId)
            ?? throw new InvalidOperationException($"Chat with ID {chatId} not found");
    }

    public async Task<Chat> CreateChatAsync(int? projectId = null, string? modelName = null)
    {
        var chat = new Chat
        {
            Title = "New Chat",
            ProjectId = projectId,
            ModelName = modelName ?? "llama3.2",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();
        return chat;
    }

    public async Task<Chat> UpdateChatAsync(Chat chat)
    {
        chat.UpdatedAt = DateTime.UtcNow;
        _context.Chats.Update(chat);
        await _context.SaveChangesAsync();
        return chat;
    }

    public async Task DeleteChatAsync(int chatId)
    {
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat != null)
        {
            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync();
        }
    }

    public async Task MoveChatToProjectAsync(int chatId, int? projectId)
    {
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat != null)
        {
            chat.ProjectId = projectId;
            chat.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Message Operations

    public async Task<List<ChatMessage>> GetMessagesAsync(int chatId)
    {
        return await _context.ChatMessages
            .Include(m => m.Attachments)
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChatMessage> AddMessageAsync(int chatId, string role, string content)
    {
        var message = new ChatMessage
        {
            ChatId = chatId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);

        // Update chat's UpdatedAt
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat != null)
        {
            chat.UpdatedAt = DateTime.UtcNow;

            // Auto-generate title from first user message
            if (chat.Title == "New Chat" && role == "user")
            {
                chat.Title = content.Length > 50 ? content[..50] + "..." : content;
            }
        }

        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<ChatMessage> UpdateMessageAsync(ChatMessage message)
    {
        _context.ChatMessages.Update(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task DeleteMessageAsync(int messageId)
    {
        var message = await _context.ChatMessages.FindAsync(messageId);
        if (message != null)
        {
            _context.ChatMessages.Remove(message);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Artifact Operations

    public async Task<List<Artifact>> GetArtifactsAsync(int chatId)
    {
        return await _context.Artifacts
            .Where(a => a.ChatId == chatId)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Artifact> CreateArtifactAsync(int chatId, string title, string content, string type, string? language = null, int? messageId = null)
    {
        var artifact = new Artifact
        {
            ChatId = chatId,
            MessageId = messageId,
            Title = title,
            Content = content,
            ArtifactType = type,
            Language = language,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Artifacts.Add(artifact);
        await _context.SaveChangesAsync();
        return artifact;
    }

    public async Task<Artifact> UpdateArtifactAsync(Artifact artifact)
    {
        artifact.UpdatedAt = DateTime.UtcNow;
        _context.Artifacts.Update(artifact);
        await _context.SaveChangesAsync();
        return artifact;
    }

    public async Task DeleteArtifactAsync(int artifactId)
    {
        var artifact = await _context.Artifacts.FindAsync(artifactId);
        if (artifact != null)
        {
            _context.Artifacts.Remove(artifact);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Artifact> CreateArtifactVersionAsync(int parentArtifactId, string content)
    {
        var parent = await _context.Artifacts.FindAsync(parentArtifactId)
            ?? throw new InvalidOperationException($"Artifact with ID {parentArtifactId} not found");

        var newVersion = new Artifact
        {
            ChatId = parent.ChatId,
            MessageId = parent.MessageId,
            Title = parent.Title,
            Content = content,
            ArtifactType = parent.ArtifactType,
            Language = parent.Language,
            Version = parent.Version + 1,
            ParentArtifactId = parentArtifactId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Artifacts.Add(newVersion);
        await _context.SaveChangesAsync();
        return newVersion;
    }

    #endregion

    #region Search

    public async Task<List<Chat>> SearchChatsAsync(string query)
    {
        var lowerQuery = query.ToLowerInvariant();

        return await _context.Chats
            .Include(c => c.Messages)
            .Where(c => !c.IsArchived &&
                (c.Title.ToLower().Contains(lowerQuery) ||
                 c.Messages.Any(m => m.Content.ToLower().Contains(lowerQuery))))
            .OrderByDescending(c => c.UpdatedAt)
            .Take(50)
            .ToListAsync();
    }

    #endregion
}
