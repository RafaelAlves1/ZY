
using ZY.Application.Services;
using ZY.Core.Interfaces;
using ZY.Core.Models;

namespace ZY.Tests;

public class ConversationServiceTests
{
    private static async Task<(
        ConversationService Service,
        FakeConversationRepository Repository
    )> CreateInitializedService(FakeAiService aiService)
    {
        var repository = new FakeConversationRepository();

        var service = new ConversationService(
            aiService,
            repository
        );

        await service.InitializeAsync();

        return (service, repository);
    }

    [Fact]
    public async Task History_ShouldBeEmpty_WhenServiceIsInitialized()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (conversationService, _) =
            await CreateInitializedService(aiService);

        // Act
        var history = conversationService.History;

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldStoreUserMessageAndAssistantResponse()
    {
        // Arrange
        var aiService = new FakeAiService
        {
            Response = "Olá! Como posso ajudar?"
        };

        var (conversationService, repository) =
            await CreateInitializedService(aiService);

        // Act
        var response = await conversationService.SendMessageAsync("Olá, ZY!");

        // Assert
        Assert.Equal("Olá! Como posso ajudar?", response);

        Assert.Collection(
            conversationService.History,
            message =>
            {
                Assert.Equal("user", message.Role);
                Assert.Equal("Olá, ZY!", message.Content);
            },
            message =>
            {
                Assert.Equal("assistant", message.Role);
                Assert.Equal("Olá! Como posso ajudar?", message.Content);
            }
        );

        Assert.Equal(2, repository.Messages.Count);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldSendPreviousMessagesToAiService()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (conversationService, _) =
            await CreateInitializedService(aiService);

        // Act
        await conversationService.SendMessageAsync("Meu nome é Rafael.");
        await conversationService.SendMessageAsync("Qual é meu nome?");

        // Assert
        Assert.Equal(3, aiService.LastReceivedHistory.Count);

        Assert.Equal(
            "Meu nome é Rafael.",
            aiService.LastReceivedHistory[0].Content
        );

        Assert.Equal(
            "Qual é meu nome?",
            aiService.LastReceivedHistory[2].Content
        );
    }

    [Fact]
    public async Task SendMessageAsync_ShouldNotPersistMessages_WhenAiServiceFails()
    {
        // Arrange
        var aiService = new FakeAiService
        {
            ShouldThrowException = true
        };

        var (conversationService, repository) =
            await CreateInitializedService(aiService);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => conversationService.SendMessageAsync("Olá, ZY!")
        );

        // Assert
        Assert.Empty(conversationService.History);
        Assert.Empty(repository.Messages);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldNotUpdateHistory_WhenRepositoryFails()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (conversationService, repository) =
            await CreateInitializedService(aiService);

        repository.ShouldFailOnAddMessages = true;

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => conversationService.SendMessageAsync("Olá, ZY!")
        );

        // Assert
        Assert.Empty(conversationService.History);
        Assert.Empty(repository.Messages);
    }

    [Fact]
    public async Task ClearHistoryAsync_ShouldRemoveAllMessages()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (conversationService, _) =
            await CreateInitializedService(aiService);

        await conversationService.SendMessageAsync("Olá, ZY!");
        await conversationService.SendMessageAsync("Como você está?");

        // Act
        await conversationService.ClearHistoryAsync();

        // Assert
        Assert.Empty(conversationService.History);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldKeepOnlyLast20Messages()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (conversationService, _) =
            await CreateInitializedService(aiService);

        // Act
        for (var i = 1; i <= 15; i++)
        {
            await conversationService.SendMessageAsync($"Mensagem {i}");
        }

        // Assert
        Assert.Equal(20, conversationService.History.Count);

        Assert.Equal(
            "Mensagem 6",
            conversationService.History[0].Content
        );

        Assert.Equal(
            "Mensagem 15",
            conversationService.History[18].Content
        );

        Assert.Equal(
            "Resposta simulada.",
            conversationService.History[19].Content
        );
    }

    [Fact]
    public async Task SendMessageAsync_ShouldSendAtMost20MessagesToAiService()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (conversationService, _) =
            await CreateInitializedService(aiService);

        // Act
        for (var i = 1; i <= 15; i++)
        {
            await conversationService.SendMessageAsync($"Mensagem {i}");
        }

        // Assert
        Assert.True(aiService.LastReceivedHistory.Count <= 20);

        Assert.Equal(19, aiService.LastReceivedHistory.Count);

        Assert.Equal(
            "Mensagem 6",
            aiService.LastReceivedHistory[0].Content
        );

        Assert.Equal(
            "Mensagem 15",
            aiService.LastReceivedHistory[18].Content
        );
    }

    private sealed class FakeAiService : IAiService
    {
        public IReadOnlyList<ChatMessage> LastReceivedHistory { get; private set; } = [];

        public bool ShouldThrowException { get; set; }

        public string Response { get; set; } = "Resposta simulada.";

        public Task<string> GenerateResponseAsync(
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            LastReceivedHistory = history.ToList();

            if (ShouldThrowException)
            {
                throw new InvalidOperationException(
                    "Falha simulada na comunicação com a IA."
                );
            }

            return Task.FromResult(Response);
        }
    }

    private sealed class FakeConversationRepository : IConversationRepository
    {
        private readonly List<Conversation> _conversations = [];
        private readonly List<ChatMessage> _messages = [];

        public IReadOnlyList<ChatMessage> Messages =>
            _messages.AsReadOnly();

        public bool ShouldFailOnAddMessages { get; set; }

        public Task<Conversation?> GetByIdAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
        {
            var conversation = _conversations
                .FirstOrDefault(item => item.Id == conversationId);

            return Task.FromResult(conversation);
        }

        public Task<Conversation?> GetMostRecentAsync(
            CancellationToken cancellationToken = default)
        {
            var conversation = _conversations
                .OrderByDescending(item => item.UpdatedAt)
                .FirstOrDefault();

            return Task.FromResult(conversation);
        }

        public Task<Conversation> CreateAsync(
            CancellationToken cancellationToken = default)
        {
            var conversation = new Conversation();

            _conversations.Add(conversation);

            return Task.FromResult(conversation);
        }

        public Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(
            Guid conversationId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ChatMessage> messages = _messages
                .Where(item => item.ConversationId == conversationId)
                .OrderByDescending(item => item.CreatedAt)
                .Take(limit)
                .Reverse()
                .ToList();

            return Task.FromResult(messages);
        }

        public Task AddMessageAsync(
            ChatMessage message,
            CancellationToken cancellationToken = default)
        {
            var conversation = _conversations
                .First(item => item.Id == message.ConversationId);

            conversation.UpdateActivity();

            _messages.Add(message);

            return Task.CompletedTask;
        }

        public Task AddMessagesAsync(
            IReadOnlyList<ChatMessage> messages,
            CancellationToken cancellationToken = default)
        {
            if (ShouldFailOnAddMessages)
            {
                throw new InvalidOperationException(
                    "Falha simulada ao persistir mensagens."
                );
            }

            foreach (var message in messages)
            {
                var conversation = _conversations.FirstOrDefault(
                    item => item.Id == message.ConversationId
                );

                if (conversation is null)
                {
                    throw new InvalidOperationException(
                        "A conversa associada à mensagem não foi encontrada."
                    );
                }
            }

            _messages.AddRange(messages);

            return Task.CompletedTask;
        }

        public Task ClearMessagesAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
        {
            _messages.RemoveAll(
                item => item.ConversationId == conversationId
            );

            return Task.CompletedTask;
        }

        public Task UpdateConversationAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
