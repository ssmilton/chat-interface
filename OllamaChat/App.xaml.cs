using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OllamaChat.Data;
using OllamaChat.Models;
using OllamaChat.Services;
using OllamaChat.ViewModels;
using OllamaChat.Views;

namespace OllamaChat;

/// <summary>
/// Application entry point
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load configuration
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        Configuration = builder.Build();

        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Initialize database
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            context.Database.EnsureCreated();
        }

        // Show main window
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var ollamaConfig = new OllamaConfig();
        Configuration.GetSection("Ollama").Bind(ollamaConfig);
        services.AddSingleton(ollamaConfig);

        // Database
        services.AddDbContext<ChatDbContext>(options =>
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OllamaChat");
            Directory.CreateDirectory(appDataPath);
            var dbPath = Path.Combine(appDataPath, "ollama_chat.db");
            options.UseSqlite($"Data Source={dbPath}");
        });

        // Services
        services.AddSingleton<IOllamaService>(sp =>
            new OllamaService(sp.GetRequiredService<OllamaConfig>()));
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<FileService>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }
}
