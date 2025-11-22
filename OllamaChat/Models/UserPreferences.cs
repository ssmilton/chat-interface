namespace OllamaChat.Models;

/// <summary>
/// User preferences that persist between sessions
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// The last selected LLM model name
    /// </summary>
    public string? LastUsedModel { get; set; }
}
