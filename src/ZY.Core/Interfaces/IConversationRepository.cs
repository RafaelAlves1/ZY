
using ZY.Core.Models;

namespace ZY.Core.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default
    );

    Task<Conversation?> GetMostRecentAsync(
        CancellationToken cancellationToken = default
    );

    Task<Conversation> CreateAsync(
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(
        Guid conversationId,
        int limit,
        CancellationToken cancellationToken = default
    );

    Task AddMessageAsync(
        ChatMessage message,
        CancellationToken cancellationToken = default
    );

    Task ClearMessagesAsync(
    Guid conversationId,
    CancellationToken cancellationToken = default
);

    Task UpdateConversationAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default
    );
    Task AddMessagesAsync(
    IReadOnlyList<ChatMessage> messages,
    CancellationToken cancellationToken = default
);
}
