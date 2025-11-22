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

        _preferences = Load();
    }

    /// <summary>
    /// Gets the last used model name, or null if not set
    /// </summary>
    public string? LastUsedModel => _preferences.LastUsedModel;

    /// <summary>
    /// Sets and persists the last used model name
    /// </summary>
    public void SetLastUsedModel(string modelName)
    {
        if (_preferences.LastUsedModel != modelName)
        {
            _preferences.LastUsedModel = modelName;
            Save();
        }
    }

    private UserPreferences Load()
    {
        try
        {
            if (File.Exists(_preferencesPath))
            {
                var json = File.ReadAllText(_preferencesPath);
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

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_preferences, _jsonOptions);
            File.WriteAllText(_preferencesPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving user preferences: {ex.Message}");
        }
    }
}
