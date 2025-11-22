using System.IO;
using System.Text.Json;
using OllamaChat.Models;

namespace OllamaChat.Services;

/// <summary>
/// Service for managing user preferences that persist between sessions
/// </summary>
public class UserPreferencesService
{
    private readonly string _preferencesPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private UserPreferences _preferences;
    private Task? _pendingSaveTask;
    private readonly object _saveLock = new();

    public UserPreferencesService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OllamaChat");
        Directory.CreateDirectory(appDataPath);
        _preferencesPath = Path.Combine(appDataPath, "user_preferences.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        _preferences = new UserPreferences();
    }

    /// <summary>
    /// Initializes the service by loading preferences asynchronously
    /// </summary>
    public async Task InitializeAsync()
    {
        _preferences = await LoadAsync();
    }

    /// <summary>
    /// Gets the last used model name, or null if not set
    /// </summary>
    public string? LastUsedModel => _preferences.LastUsedModel;

    /// <summary>
    /// Sets and persists the last used model name (non-blocking)
    /// </summary>
    public void SetLastUsedModel(string modelName)
    {
        if (_preferences.LastUsedModel != modelName)
        {
            _preferences.LastUsedModel = modelName;
            // Fire-and-forget save on background thread to avoid UI lag
            SaveInBackground();
        }
    }

    private async Task<UserPreferences> LoadAsync()
    {
        try
        {
            if (File.Exists(_preferencesPath))
            {
                var json = await File.ReadAllTextAsync(_preferencesPath);
                return JsonSerializer.Deserialize<UserPreferences>(json, _jsonOptions)
                    ?? new UserPreferences();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading user preferences: {ex.Message}");
        }

        return new UserPreferences();
    }

    private void SaveInBackground()
    {
        lock (_saveLock)
        {
            // Cancel any pending save and start a new one
            _pendingSaveTask = Task.Run(async () =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(_preferences, _jsonOptions);
                    await File.WriteAllTextAsync(_preferencesPath, json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving user preferences: {ex.Message}");
                }
            });
        }
    }
}
