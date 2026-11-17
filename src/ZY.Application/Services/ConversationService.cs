
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

    public Guid? ActiveConversationId => _activeConversation?.Id;

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

        var conversationTitle = _activeConversation.Title == "Nova conversa"
    ? GenerateConversationTitle(message)
    : null;

        await _conversationRepository.AddMessagesAsync(
            [userMessage, assistantMessage],
            cancellationToken,
            conversationTitle
        );

        if (conversationTitle is not null)
            _activeConversation.UpdateTitle(conversationTitle);

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

    public async Task<IReadOnlyList<Conversation>> GetConversationsAsync(
    CancellationToken cancellationToken = default)
    {
        return await _conversationRepository.GetAllAsync(cancellationToken);
    }

    public async Task CreateConversationAsync(
        CancellationToken cancellationToken = default)
    {
        _activeConversation = await _conversationRepository.CreateAsync(
            cancellationToken
        );

        _history.Clear();
    }

    public async Task SelectConversationAsync(
    Guid conversationId,
    CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(
            conversationId,
            cancellationToken
        );

        if (conversation is null)
            throw new InvalidOperationException(
                "A conversa selecionada não foi encontrada."
            );

        _activeConversation = conversation;

        await LoadHistoryAsync(cancellationToken);
    }



    private static string GenerateConversationTitle(string message)
    {
        var normalizedMessage = string.Join(
            " ",
            message.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries
            )
        );

        const int maxLength = 60;

        if (normalizedMessage.Length <= maxLength)
            return normalizedMessage;

        var title = normalizedMessage[..(maxLength - 3)];

        var lastSpace = title.LastIndexOf(' ');

        if (lastSpace > 0)
            title = title[..lastSpace];

        return title.TrimEnd('.', ',', ';', ':', '!', '?') + "...";
    }


}
