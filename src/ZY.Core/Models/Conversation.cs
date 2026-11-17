
namespace ZY.Core.Models;

/// <summary>
/// Representa uma conversa armazenada pelo ZY.
/// </summary>
public sealed class Conversation
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();

    public void UpdateActivity()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
