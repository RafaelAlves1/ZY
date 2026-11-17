
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

    Guid? ActiveConversationId { get; }

    Task<IReadOnlyList<Conversation>> GetConversationsAsync(
        CancellationToken cancellationToken = default
    );

    Task CreateConversationAsync(
        CancellationToken cancellationToken = default
    );

    Task SelectConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default
    );
}
