namespace OllamaChat.Models;

/// <summary>
/// Configuration settings for Ollama API connection
/// </summary>
public class OllamaConfig
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string DefaultModel { get; set; } = "llama3.2";
    public int TimeoutSeconds { get; set; } = 300;
    public bool StreamResponses { get; set; } = true;
    public OllamaModelOptions DefaultOptions { get; set; } = new();
}

/// <summary>
/// Model generation options
/// </summary>
public class OllamaModelOptions
{
    public float Temperature { get; set; } = 0.7f;
    public int NumCtx { get; set; } = 4096;
    public float TopP { get; set; } = 0.9f;
    public int TopK { get; set; } = 40;
    public float RepeatPenalty { get; set; } = 1.1f;
    public int? NumPredict { get; set; }
    public string[]? Stop { get; set; }
}

/// <summary>
/// Ollama API request for chat completion
/// </summary>
public class OllamaChatRequest
{
    public string Model { get; set; } = string.Empty;
    public List<OllamaChatMessage> Messages { get; set; } = new();
    public bool Stream { get; set; } = true;
    public OllamaModelOptions? Options { get; set; }
}

/// <summary>
/// Message format for Ollama API
/// </summary>
public class OllamaChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string[]? Images { get; set; }
}

/// <summary>
/// Ollama API response for chat completion
/// </summary>
public class OllamaChatResponse
{
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public OllamaChatMessage? Message { get; set; }
    public bool Done { get; set; }
    public long TotalDuration { get; set; }
    public long LoadDuration { get; set; }
    public int PromptEvalCount { get; set; }
    public long PromptEvalDuration { get; set; }
    public int EvalCount { get; set; }
    public long EvalDuration { get; set; }
}

/// <summary>
/// Ollama model information
/// </summary>
public class OllamaModel
{
    public string Name { get; set; } = string.Empty;
    public DateTime ModifiedAt { get; set; }
    public long Size { get; set; }
    public string Digest { get; set; } = string.Empty;
}

/// <summary>
/// Response from Ollama list models API
/// </summary>
public class OllamaModelsResponse
{
    public List<OllamaModel> Models { get; set; } = new();
}
