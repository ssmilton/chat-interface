namespace OllamaChat.Models;

/// <summary>
/// Configuration settings for web search functionality
/// </summary>
public class WebSearchConfig
{
    /// <summary>
    /// Whether web search is enabled by default
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of search results to retrieve
    /// </summary>
    public int MaxResults { get; set; } = 5;

    /// <summary>
    /// Timeout in seconds for search requests
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// User agent string for HTTP requests
    /// </summary>
    public string UserAgent { get; set; } = "OllamaChat/1.0 (Windows; Desktop Application)";

    /// <summary>
    /// Search provider to use (DuckDuckGo, Google, Bing)
    /// </summary>
    public string Provider { get; set; } = "DuckDuckGo";
}

/// <summary>
/// Represents a single search result from web search
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Title of the search result
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL of the search result
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Snippet/description of the search result
    /// </summary>
    public string Snippet { get; set; } = string.Empty;

    /// <summary>
    /// Source/domain of the result
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Position in search results (1-indexed)
    /// </summary>
    public int Position { get; set; }
}

/// <summary>
/// Response containing search results and metadata
/// </summary>
public class WebSearchResponse
{
    /// <summary>
    /// The original search query
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// List of search results
    /// </summary>
    public List<SearchResult> Results { get; set; } = new();

    /// <summary>
    /// Whether the search was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if search failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Time taken to perform the search in milliseconds
    /// </summary>
    public long SearchDurationMs { get; set; }

    /// <summary>
    /// Formats the search results as context for the LLM
    /// </summary>
    public string ToContextString()
    {
        if (!Success || Results.Count == 0)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[Web Search Results for: \"{Query}\"]");
        sb.AppendLine();

        foreach (var result in Results)
        {
            sb.AppendLine($"{result.Position}. {result.Title}");
            sb.AppendLine($"   URL: {result.Url}");
            sb.AppendLine($"   {result.Snippet}");
            sb.AppendLine();
        }

        sb.AppendLine("[End of Search Results]");
        return sb.ToString();
    }
}
