
using ZY.Core.Interfaces;
using ZY.Core.Models;

namespace ZY.Application.Services;

public sealed class ConversationService : IConversationService
{
    private const int MaxHistoryMessages = 20;

    private readonly IAiService _aiService;
    private readonly IConversationRepository _conversationRepository;

    private Conversation? _activeConversation;
    private readonly List<ChatMessage> _history = [];

    public IReadOnlyList<ChatMessage> History => _history.AsReadOnly();

    public ConversationService(
        IAiService aiService,
        IConversationRepository conversationRepository)
    {
        _aiService = aiService;
        _conversationRepository = conversationRepository;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        _activeConversation =
            await _conversationRepository.GetMostRecentAsync(cancellationToken);

        if (_activeConversation is null)
        {
            _activeConversation =
                await _conversationRepository.CreateAsync(cancellationToken);
        }

        await LoadHistoryAsync(cancellationToken);
    }

    public async Task<string> SendMessageAsync(
    string message,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException(
                "A mensagem não pode estar vazia.",
                nameof(message)
            );

        if (_activeConversation is null)
            throw new InvalidOperationException(
                "O serviço de conversas não foi inicializado."
            );

        var userMessage = new ChatMessage(
            _activeConversation.Id,
            "user",
            message
        );

        var temporaryHistory = _history
            .Append(userMessage)
            .ToList();

        TrimHistory(temporaryHistory);

        var response = await _aiService.GenerateResponseAsync(
            temporaryHistory.AsReadOnly(),
            cancellationToken
        );

        var assistantMessage = new ChatMessage(
            _activeConversation.Id,
            "assistant",
            response
        );

        await _conversationRepository.AddMessagesAsync(
            [userMessage, assistantMessage],
            cancellationToken
        );

        _history.Add(userMessage);
        _history.Add(assistantMessage);

        TrimHistory(_history);

        return response;
    }

    public async Task ClearHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        if (_activeConversation is null)
            return;

        await _conversationRepository.ClearMessagesAsync(
            _activeConversation.Id,
            cancellationToken
        );

        _history.Clear();
    }

    private async Task LoadHistoryAsync(
        CancellationToken cancellationToken)
    {
        if (_activeConversation is null)
            return;

        var messages =
            await _conversationRepository.GetRecentMessagesAsync(
                _activeConversation.Id,
                MaxHistoryMessages,
                cancellationToken
            );

        _history.Clear();
        _history.AddRange(messages);
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
