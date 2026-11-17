
namespace ZY.Core.Models;

/// <summary>
/// Representa uma mensagem dentro de uma conversa com o ZY.
/// </summary>
public sealed class ChatMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ConversationId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Conversation Conversation { get; private set; } = null!;

    private ChatMessage()
    {
    }

    public ChatMessage(Guid conversationId, string role, string content)
    {
        ConversationId = conversationId;
        Role = role;
        Content = content;
    }
}
