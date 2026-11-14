
namespace ZY.Infrastructure.AI;

/// <summary>
/// Representa uma falha causada pela ausência do modelo solicitado no Ollama.
/// </summary>
public sealed class OllamaModelNotFoundException : Exception
{
    public OllamaModelNotFoundException(string message)
        : base(message)
    {
    }
}
