using OllamaChat.Models;

namespace OllamaChat.Services;

/// <summary>
/// Interface for Ollama API service
/// </summary>
public interface IOllamaService
{
    /// <summary>
    /// Get list of available models
    /// </summary>
    Task<List<OllamaModel>> GetModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a chat message and get a complete response
    /// </summary>
    Task<OllamaChatResponse> ChatAsync(OllamaChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a chat message and stream the response
    /// </summary>
    IAsyncEnumerable<OllamaChatResponse> ChatStreamAsync(OllamaChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the Ollama server is reachable
    /// </summary>
    Task<bool> IsServerAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current Ollama configuration
    /// </summary>
    OllamaConfig GetConfig();

    /// <summary>
    /// Update the Ollama configuration
    /// </summary>
    void UpdateConfig(OllamaConfig config);
}
