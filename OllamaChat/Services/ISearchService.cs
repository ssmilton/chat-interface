using OllamaChat.Models;

namespace OllamaChat.Services;

/// <summary>
/// Interface for web search service
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Perform a web search for the given query
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search response with results</returns>
    Task<WebSearchResponse> SearchAsync(string query, int? maxResults = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the search service is available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if service is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current search configuration
    /// </summary>
    WebSearchConfig GetConfig();

    /// <summary>
    /// Update the search configuration
    /// </summary>
    void UpdateConfig(WebSearchConfig config);
}
