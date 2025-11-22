using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using OllamaChat.Models;

namespace OllamaChat.Services;

/// <summary>
/// Web search service using DuckDuckGo
/// </summary>
public class WebSearchService : ISearchService, IDisposable
{
    private readonly HttpClient _httpClient;
    private WebSearchConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;

    // DuckDuckGo HTML search URL
    private const string DuckDuckGoSearchUrl = "https://html.duckduckgo.com/html/";
    // DuckDuckGo Instant Answer API
    private const string DuckDuckGoApiUrl = "https://api.duckduckgo.com/";

    public WebSearchService(WebSearchConfig config)
    {
        _config = config;

        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgent);
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public WebSearchConfig GetConfig() => _config;

    public void UpdateConfig(WebSearchConfig config)
    {
        _config = config;
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(DuckDuckGoApiUrl + "?q=test&format=json", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<WebSearchResponse> SearchAsync(string query, int? maxResults = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new WebSearchResponse { Query = query };

        try
        {
            var results = await SearchDuckDuckGoAsync(query, maxResults ?? _config.MaxResults, cancellationToken);
            response.Results = results;
            response.Success = true;
        }
        catch (OperationCanceledException)
        {
            response.Success = false;
            response.ErrorMessage = "Search was cancelled";
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.ErrorMessage = $"Search failed: {ex.Message}";
            Debug.WriteLine($"Search error: {ex}");
        }

        stopwatch.Stop();
        response.SearchDurationMs = stopwatch.ElapsedMilliseconds;

        return response;
    }

    private async Task<List<SearchResult>> SearchDuckDuckGoAsync(string query, int maxResults, CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();

        // Use DuckDuckGo HTML interface for search results
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("q", query),
            new KeyValuePair<string, string>("kl", "us-en")
        });

        var response = await _httpClient.PostAsync(DuckDuckGoSearchUrl, formContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        // Parse HTML search results using regex
        // DuckDuckGo HTML results are in div class="result results_links results_links_deep web-result"
        var resultPattern = @"<div[^>]*class=""[^""]*result[^""]*results_links[^""]*""[^>]*>(.*?)</div>\s*(?=<div[^>]*class=""[^""]*result|$)";
        var titlePattern = @"<a[^>]*class=""result__a""[^>]*href=""([^""]+)""[^>]*>([^<]*(?:<[^/][^>]*>[^<]*</[^>]*>)*[^<]*)</a>";
        var snippetPattern = @"<a[^>]*class=""result__snippet""[^>]*>([^<]*(?:<[^/][^>]*>[^<]*</[^>]*>)*[^<]*)</a>";

        // Alternative simpler patterns that work better with DuckDuckGo's HTML
        var linkMatches = Regex.Matches(html, @"<a[^>]*class=""result__a""[^>]*href=""([^""]+)""[^>]*>(.*?)</a>", RegexOptions.Singleline);
        var snippetMatches = Regex.Matches(html, @"<a[^>]*class=""result__snippet""[^>]*>(.*?)</a>", RegexOptions.Singleline);

        var position = 1;
        for (int i = 0; i < Math.Min(linkMatches.Count, maxResults); i++)
        {
            var linkMatch = linkMatches[i];
            var url = linkMatch.Groups[1].Value;
            var title = CleanHtml(linkMatch.Groups[2].Value);

            // Decode DuckDuckGo redirect URLs
            url = DecodeRedirectUrl(url);

            var snippet = i < snippetMatches.Count
                ? CleanHtml(snippetMatches[i].Groups[1].Value)
                : string.Empty;

            // Skip if URL is empty or invalid
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
                continue;

            results.Add(new SearchResult
            {
                Title = title,
                Url = url,
                Snippet = snippet,
                Source = uri.Host,
                Position = position++
            });

            if (results.Count >= maxResults)
                break;
        }

        // If HTML parsing didn't work well, try the Instant Answer API for supplementary info
        if (results.Count == 0)
        {
            results = await SearchDuckDuckGoApiAsync(query, maxResults, cancellationToken);
        }

        return results;
    }

    private async Task<List<SearchResult>> SearchDuckDuckGoApiAsync(string query, int maxResults, CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();

        var encodedQuery = HttpUtility.UrlEncode(query);
        var url = $"{DuckDuckGoApiUrl}?q={encodedQuery}&format=json&no_html=1&skip_disambig=1";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<DuckDuckGoApiResponse>(json, _jsonOptions);

        if (data == null) return results;

        var position = 1;

        // Add Abstract if available
        if (!string.IsNullOrEmpty(data.Abstract) && !string.IsNullOrEmpty(data.AbstractUrl))
        {
            results.Add(new SearchResult
            {
                Title = data.Heading ?? "Result",
                Url = data.AbstractUrl,
                Snippet = data.Abstract,
                Source = data.AbstractSource ?? "DuckDuckGo",
                Position = position++
            });
        }

        // Add Related Topics
        if (data.RelatedTopics != null)
        {
            foreach (var topic in data.RelatedTopics.Take(maxResults - results.Count))
            {
                if (!string.IsNullOrEmpty(topic.FirstUrl) && !string.IsNullOrEmpty(topic.Text))
                {
                    Uri.TryCreate(topic.FirstUrl, UriKind.Absolute, out var topicUri);
                    results.Add(new SearchResult
                    {
                        Title = ExtractTitleFromText(topic.Text),
                        Url = topic.FirstUrl,
                        Snippet = topic.Text,
                        Source = topicUri?.Host ?? "DuckDuckGo",
                        Position = position++
                    });
                }

                if (results.Count >= maxResults)
                    break;
            }
        }

        // Add Results (web results)
        if (data.Results != null)
        {
            foreach (var result in data.Results.Take(maxResults - results.Count))
            {
                if (!string.IsNullOrEmpty(result.FirstUrl))
                {
                    Uri.TryCreate(result.FirstUrl, UriKind.Absolute, out var resultUri);
                    results.Add(new SearchResult
                    {
                        Title = result.Text ?? "Result",
                        Url = result.FirstUrl,
                        Snippet = result.Text ?? string.Empty,
                        Source = resultUri?.Host ?? "Web",
                        Position = position++
                    });
                }

                if (results.Count >= maxResults)
                    break;
            }
        }

        return results;
    }

    private static string DecodeRedirectUrl(string url)
    {
        // DuckDuckGo wraps URLs in a redirect, extract the actual URL
        if (url.Contains("uddg="))
        {
            var match = Regex.Match(url, @"uddg=([^&]+)");
            if (match.Success)
            {
                return HttpUtility.UrlDecode(match.Groups[1].Value);
            }
        }

        // Also handle //duckduckgo.com/l/?kh=-1&uddg= format
        if (url.StartsWith("//"))
        {
            url = "https:" + url;
        }

        return url;
    }

    private static string CleanHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        // Remove HTML tags
        var text = Regex.Replace(html, @"<[^>]+>", " ");
        // Decode HTML entities
        text = HttpUtility.HtmlDecode(text);
        // Clean up whitespace
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }

    private static string ExtractTitleFromText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "Result";

        // Take first sentence or first N characters as title
        var firstSentence = text.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.None).FirstOrDefault();
        if (!string.IsNullOrEmpty(firstSentence) && firstSentence.Length <= 100)
        {
            return firstSentence;
        }

        return text.Length > 60 ? text.Substring(0, 57) + "..." : text;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    // DuckDuckGo API response classes
    private class DuckDuckGoApiResponse
    {
        public string? Abstract { get; set; }
        public string? AbstractText { get; set; }
        public string? AbstractSource { get; set; }
        public string? AbstractUrl { get; set; }
        public string? Heading { get; set; }
        public string? Answer { get; set; }
        public string? AnswerType { get; set; }
        public List<DuckDuckGoTopic>? RelatedTopics { get; set; }
        public List<DuckDuckGoResult>? Results { get; set; }
    }

    private class DuckDuckGoTopic
    {
        public string? Text { get; set; }
        public string? FirstUrl { get; set; }
        public DuckDuckGoIcon? Icon { get; set; }
    }

    private class DuckDuckGoResult
    {
        public string? Text { get; set; }
        public string? FirstUrl { get; set; }
    }

    private class DuckDuckGoIcon
    {
        public string? Url { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }
}
