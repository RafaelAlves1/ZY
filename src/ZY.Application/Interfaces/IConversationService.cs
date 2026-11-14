
using ZY.Core.Models;

namespace ZY.Application.Interfaces;

public interface IConversationService
{
    IReadOnlyList<ChatMessage> History { get; }

    Task<string> SendMessageAsync(
        string message,
        CancellationToken cancellationToken = default
    );

    void ClearHistory();
}
