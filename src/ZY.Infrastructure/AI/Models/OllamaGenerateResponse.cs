
using System.Text.Json.Serialization;

namespace ZY.Infrastructure.AI.Models;

/// <summary>
/// Representa a resposta recebida do endpoint de geração do Ollama.
/// </summary>
public sealed class OllamaGenerateResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}
