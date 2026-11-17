
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
public async Task SendMessageAsync_ShouldNotUpdateTitleWhenRepositoryFails()
{
    // Arrange
    var aiService = new FakeAiService();

    var (service, repository) =
        await CreateInitializedService(aiService);

    repository.ShouldFailOnAddMessages = true;

    // Act
    await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.SendMessageAsync("Quero criar uma API em C#.")
    );

    // Assert
    var conversation = Assert.Single(
        await repository.GetAllAsync()
    );

    Assert.Equal("Nova conversa", conversation.Title);
    Assert.Empty(repository.Messages);
    Assert.Empty(service.History);
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


    [Fact]
    public async Task CreateConversationAsync_ShouldCreateNewConversationAndClearHistory()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (service, repository) =
            await CreateInitializedService(aiService);

        await service.SendMessageAsync("Conversa anterior");

        var previousConversationId = service.ActiveConversationId;

        // Act
        await service.CreateConversationAsync();

        // Assert
        Assert.NotNull(service.ActiveConversationId);
        Assert.NotEqual(previousConversationId, service.ActiveConversationId);
        Assert.Empty(service.History);
        Assert.Equal(2, repository.Messages.Count);
        Assert.Equal(2, (await repository.GetAllAsync()).Count);
    }

    [Fact]
    public async Task GetConversationsAsync_ShouldReturnAllConversations()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (service, _) =
            await CreateInitializedService(aiService);

        await service.CreateConversationAsync();
        await service.CreateConversationAsync();

        // Act
        var conversations = await service.GetConversationsAsync();

        // Assert
        Assert.Equal(3, conversations.Count);
    }

    [Fact]
    public async Task SelectConversationAsync_ShouldLoadSelectedConversationHistory()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (service, _) =
            await CreateInitializedService(aiService);

        await service.SendMessageAsync("Mensagem da primeira conversa");

        var firstConversationId = service.ActiveConversationId!.Value;

        await service.CreateConversationAsync();

        await service.SendMessageAsync("Mensagem da segunda conversa");

        // Act
        await service.SelectConversationAsync(firstConversationId);

        // Assert
        Assert.Equal(firstConversationId, service.ActiveConversationId);
        Assert.Equal(2, service.History.Count);
        Assert.Equal(
            "Mensagem da primeira conversa",
            service.History[0].Content
        );
    }

    [Fact]
    public async Task SelectConversationAsync_ShouldNotChangeActiveConversation_WhenIdDoesNotExist()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (service, _) =
            await CreateInitializedService(aiService);

        await service.SendMessageAsync("Mensagem preservada");

        var activeConversationId = service.ActiveConversationId;

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SelectConversationAsync(Guid.NewGuid())
        );

        // Assert
        Assert.Equal(activeConversationId, service.ActiveConversationId);
        Assert.Equal(2, service.History.Count);
        Assert.Equal("Mensagem preservada", service.History[0].Content);
    }


    [Fact]
    public async Task SendMessageAsync_ShouldNotMixMessagesFromDifferentConversations()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (service, _) =
            await CreateInitializedService(aiService);

        await service.SendMessageAsync("Mensagem da conversa A");

        var conversationAId = service.ActiveConversationId!.Value;

        await service.CreateConversationAsync();

        await service.SendMessageAsync("Mensagem da conversa B");

        // Act
        await service.SelectConversationAsync(conversationAId);

        await service.SendMessageAsync("Outra mensagem da conversa A");

        // Assert
        Assert.Equal(3, aiService.LastReceivedHistory.Count);

        Assert.Equal(
            "Mensagem da conversa A",
            aiService.LastReceivedHistory[0].Content
        );

        Assert.Equal(
            "Outra mensagem da conversa A",
            aiService.LastReceivedHistory[2].Content
        );

        Assert.DoesNotContain(
            aiService.LastReceivedHistory,
            message => message.Content == "Mensagem da conversa B"
        );
    }


    [Fact]
    public async Task SendMessageAsync_ShouldGenerateTitleFromFirstMessage()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (service, repository) =
            await CreateInitializedService(aiService);

        // Act
        await service.SendMessageAsync("Quero criar uma API em C#.");

        // Assert
        var conversation = Assert.Single(
            await repository.GetAllAsync()
        );

        Assert.Equal("Quero criar uma API em C#.", conversation.Title);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldNotChangeTitleAfterFirstMessage()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (service, repository) =
            await CreateInitializedService(aiService);

        // Act
        await service.SendMessageAsync("Primeira mensagem da conversa.");

        var conversation = Assert.Single(
            await repository.GetAllAsync()
        );

        var originalTitle = conversation.Title;

        await service.SendMessageAsync("Uma segunda mensagem diferente.");

        // Assert
        Assert.Equal(originalTitle, conversation.Title);
    }


    [Fact]
    public async Task SendMessageAsync_ShouldLimitGeneratedTitleTo60Characters()
    {
        // Arrange
        var aiService = new FakeAiService();

        var (service, repository) =
            await CreateInitializedService(aiService);

        var message =
            "Quero desenvolver uma aplicação completa para gerenciamento financeiro pessoal utilizando C# e PostgreSQL.";

        // Act
        await service.SendMessageAsync(message);

        // Assert
        var conversation = Assert.Single(
            await repository.GetAllAsync()
        );

        Assert.True(conversation.Title.Length <= 60);
        Assert.EndsWith("...", conversation.Title);
        Assert.False(conversation.Title.EndsWith(" ", StringComparison.Ordinal));
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

        public Task<IReadOnlyList<Conversation>> GetAllAsync(
    CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Conversation> conversations = _conversations
                .OrderByDescending(conversation => conversation.UpdatedAt)
                .ToList();

            return Task.FromResult(conversations);
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
            CancellationToken cancellationToken = default,
            string? conversationTitle = null)
        {
            if (ShouldFailOnAddMessages)
            {
                throw new InvalidOperationException(
                    "Falha simulada ao persistir mensagens."
                );
            }

            if (messages.Count == 0)
                return Task.CompletedTask;

            var conversationId = messages[0].ConversationId;

            if (messages.Any(message => message.ConversationId != conversationId))
            {
                throw new InvalidOperationException(
                    "Todas as mensagens devem pertencer à mesma conversa."
                );
            }

            var conversation = _conversations.FirstOrDefault(
                item => item.Id == conversationId
            );

            if (conversation is null)
            {
                throw new InvalidOperationException(
                    "A conversa associada às mensagens não foi encontrada."
                );
            }

            if (!string.IsNullOrWhiteSpace(conversationTitle))
                conversation.UpdateTitle(conversationTitle);

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
