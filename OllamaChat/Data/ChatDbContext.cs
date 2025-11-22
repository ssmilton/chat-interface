using Microsoft.EntityFrameworkCore;
using OllamaChat.Models;
using System.IO;

namespace OllamaChat.Data;

/// <summary>
/// Entity Framework database context for the chat application
/// </summary>
public class ChatDbContext : DbContext
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Artifact> Artifacts { get; set; }
    public DbSet<FileAttachment> FileAttachments { get; set; }

    private readonly string _dbPath;

    public ChatDbContext()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OllamaChat");

        Directory.CreateDirectory(appDataPath);
        _dbPath = Path.Combine(appDataPath, "ollama_chat.db");
    }

    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OllamaChat");

        Directory.CreateDirectory(appDataPath);
        _dbPath = Path.Combine(appDataPath, "ollama_chat.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Project self-referencing relationship
        modelBuilder.Entity<Project>()
            .HasOne(p => p.ParentProject)
            .WithMany(p => p.ChildProjects)
            .HasForeignKey(p => p.ParentProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Chat-Project relationship
        modelBuilder.Entity<Chat>()
            .HasOne(c => c.Project)
            .WithMany(p => p.Chats)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        // ChatMessage-Chat relationship
        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.Chat)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        // Artifact-Chat relationship
        modelBuilder.Entity<Artifact>()
            .HasOne(a => a.Chat)
            .WithMany(c => c.Artifacts)
            .HasForeignKey(a => a.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        // Artifact version tracking
        modelBuilder.Entity<Artifact>()
            .HasOne(a => a.ParentArtifact)
            .WithMany(a => a.ChildArtifacts)
            .HasForeignKey(a => a.ParentArtifactId)
            .OnDelete(DeleteBehavior.Restrict);

        // FileAttachment-ChatMessage relationship
        modelBuilder.Entity<FileAttachment>()
            .HasOne(f => f.Message)
            .WithMany(m => m.Attachments)
            .HasForeignKey(f => f.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for better query performance
        modelBuilder.Entity<Chat>()
            .HasIndex(c => c.UpdatedAt);

        modelBuilder.Entity<Chat>()
            .HasIndex(c => c.ProjectId);

        modelBuilder.Entity<ChatMessage>()
            .HasIndex(m => m.ChatId);

        modelBuilder.Entity<Artifact>()
            .HasIndex(a => a.ChatId);
    }
}
