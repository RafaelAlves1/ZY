
namespace ZY.Core.Models;

/// <summary>
/// Representa uma conversa armazenada pelo ZY.
/// </summary>
public sealed class Conversation
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Title { get; private set; } = "Nova conversa";

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();


    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(
                "O título da conversa não pode estar vazio.",
                nameof(title)
            );

        if (Title != "Nova conversa")
            return;

        Title = title.Trim();
    }


    public void UpdateActivity()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
