
namespace ZY.Core.Models;

/// <summary>
/// Representa uma mensagem dentro de uma conversa com o ZY.
/// </summary>
public sealed record ChatMessage(
    string Role,
    string Content
);
