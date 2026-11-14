
using System.Net.Http.Json;
using System.Text.Json;
using ZY.Application.AI;
using ZY.Core.Interfaces;
using ZY.Core.Models;

namespace ZY.Infrastructure.AI;

public sealed class OllamaAiService : IAiService
{
    private readonly HttpClient _httpClient;

    private const string ModelName = "qwen2.5:1.5b";

    public OllamaAiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default
    )
    {
        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = ZYSystemPrompt.Instructions
            }
        };

        messages.AddRange(
            history.Select(message => (object)new
            {
                role = message.Role,
                content = message.Content
            })
        );

        var request = new
        {
            model = ModelName,
            messages,
            stream = false
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "/api/chat",
            request,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        using var jsonDocument = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken
        );

        var content = jsonDocument
            .RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? string.Empty;
    }
}
