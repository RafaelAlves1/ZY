
using ZY.Core.Models;

namespace ZY.Core.Interfaces;

/// <summary>
/// Define o contrato para comunicação com um serviço de inteligência artificial.
/// </summary>
public interface IAiService
{
    Task<string> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default
    );
}
