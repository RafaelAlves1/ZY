
using ZY.Application.Interfaces;
using ZY.Core.Interfaces;
using ZY.Core.Models;

namespace ZY.Application.Services;

public sealed class ConversationService : IConversationService
{
    private const int MaxHistoryMessages = 20;

    private readonly IAiService _aiService;

    private readonly List<ChatMessage> _history = [];

    public IReadOnlyList<ChatMessage> History => _history.AsReadOnly();

    public ConversationService(IAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<string> SendMessageAsync(
        string message,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException(
                "A mensagem não pode estar vazia.",
                nameof(message)
            );

        _history.Add(new ChatMessage("user", message));

        try
        {
            var historyToSend = _history.ToList();

            TrimHistory(historyToSend);

            var response = await _aiService.GenerateResponseAsync(
                historyToSend.AsReadOnly(),
                cancellationToken
            );

            _history.Add(new ChatMessage("assistant", response));

            TrimHistory(_history);

            return response;
        }
        catch
        {
            _history.RemoveAt(_history.Count - 1);
            throw;
        }
    }

    public void ClearHistory()
    {
        _history.Clear();
    }

    private static void TrimHistory(List<ChatMessage> history)
    {
        while (history.Count > MaxHistoryMessages)
        {
            var messagesToRemove = Math.Min(2, history.Count);

            history.RemoveRange(0, messagesToRemove);
        }
    }
}
