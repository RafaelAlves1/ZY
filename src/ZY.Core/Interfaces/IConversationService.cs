
using ZY.Core.Models;

namespace ZY.Core.Interfaces;

public interface IConversationService
{
    IReadOnlyList<ChatMessage> History { get; }

    Task InitializeAsync(
        CancellationToken cancellationToken = default
    );

    Task<string> SendMessageAsync(
        string message,
        CancellationToken cancellationToken = default
    );

    Task ClearHistoryAsync(
        CancellationToken cancellationToken = default
    );
}
