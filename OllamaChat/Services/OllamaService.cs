using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OllamaChat.Models;
using System.IO;

namespace OllamaChat.Services;

/// <summary>
/// Service for interacting with the Ollama API
/// </summary>
public class OllamaService : IOllamaService, IDisposable
{
    private readonly HttpClient _httpClient;
    private OllamaConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public OllamaService(OllamaConfig config)
    {
        _config = config;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_config.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }

    public OllamaConfig GetConfig() => _config;

    public void UpdateConfig(OllamaConfig config)
    {
        _config = config;
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    public async Task<bool> IsServerAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<OllamaModel>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaModelsResponse>(_jsonOptions, cancellationToken);
            return result?.Models ?? new List<OllamaModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting models: {ex.Message}");
            return new List<OllamaModel>();
        }
    }

    public async Task<OllamaChatResponse> ChatAsync(OllamaChatRequest request, CancellationToken cancellationToken = default)
    {
        request.Stream = false;
        request.Options ??= _config.DefaultOptions;

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("/api/chat", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(_jsonOptions, cancellationToken);
        return result ?? new OllamaChatResponse();
    }

    public async IAsyncEnumerable<OllamaChatResponse> ChatStreamAsync(
        OllamaChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        request.Stream = true;
        request.Options ??= _config.DefaultOptions;

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaChatResponse? chatResponse = null;
            try
            {
                chatResponse = JsonSerializer.Deserialize<OllamaChatResponse>(line, _jsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (chatResponse != null)
            {
                yield return chatResponse;

                if (chatResponse.Done)
                {
                    yield break;
                }
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
