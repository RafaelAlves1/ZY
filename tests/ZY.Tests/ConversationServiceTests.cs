
using ZY.Application.Services;
using ZY.Core.Interfaces;
using ZY.Core.Models;

namespace ZY.Tests;

public class ConversationServiceTests
{
    [Fact]
    public void History_ShouldBeEmpty_WhenServiceIsCreated()
    {
        // Arrange
        var aiService = new FakeAiService();
        var conversationService = new ConversationService(aiService);

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

        var conversationService = new ConversationService(aiService);

        // Act
        var response = await conversationService.SendMessageAsync(
            "Olá, ZY!"
        );

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
    }

    private sealed class FakeAiService : IAiService
    {
        public IReadOnlyList<ChatMessage> LastReceivedHistory { get; private set; }
    = [];

        public bool ShouldThrowException { get; set; }

        public string Response { get; set; } = "Resposta simulada.";

        public Task<string> GenerateResponseAsync(
     IReadOnlyList<ChatMessage> history,
     CancellationToken cancellationToken = default
 )
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


    [Fact]
    public async Task SendMessageAsync_ShouldSendPreviousMessagesToAiService()
    {
        // Arrange
        var aiService = new FakeAiService();
        var conversationService = new ConversationService(aiService);

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
    public async Task SendMessageAsync_ShouldRemoveUserMessage_WhenAiServiceFails()
    {
        // Arrange
        var aiService = new FakeAiService
        {
            ShouldThrowException = true
        };

        var conversationService = new ConversationService(aiService);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => conversationService.SendMessageAsync("Olá, ZY!")
        );

        // Assert
        Assert.Empty(conversationService.History);
    }


    [Fact]
    public async Task ClearHistory_ShouldRemoveAllMessages()
    {
        // Arrange
        var aiService = new FakeAiService();
        var conversationService = new ConversationService(aiService);

        await conversationService.SendMessageAsync("Olá, ZY!");
        await conversationService.SendMessageAsync("Como você está?");

        // Act
        conversationService.ClearHistory();

        // Assert
        Assert.Empty(conversationService.History);
    }


    [Fact]
    public async Task SendMessageAsync_ShouldKeepOnlyLast20Messages()
    {
        // Arrange
        var aiService = new FakeAiService();
        var conversationService = new ConversationService(aiService);

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
        var conversationService = new ConversationService(aiService);

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




}
