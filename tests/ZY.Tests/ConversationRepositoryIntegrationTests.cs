
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ZY.Core.Models;
using ZY.Infrastructure.Persistence.PostgreSQL.Context;
using ZY.Infrastructure.Persistence.PostgreSQL.Repositories;

namespace ZY.Tests;

public sealed class ConversationRepositoryIntegrationTests
    : IClassFixture<PostgreSqlTestFixture>
{
    private readonly PostgreSqlTestFixture _fixture;

    public ConversationRepositoryIntegrationTests(
        PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddMessagesAsync_ShouldPersistMessagesAndTitleAtomically()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        var repository = new ConversationRepository(context);

        var conversation = await repository.CreateAsync();

        var messages = new List<ChatMessage>
        {
            new(conversation.Id, "user", "Quero criar uma API em C#."),
            new(conversation.Id, "assistant", "Vamos começar!")
        };

        // Act
        await repository.AddMessagesAsync(
            messages,
            conversationTitle: "Quero criar uma API em C#."
        );

        // Assert
        await using var verificationContext = _fixture.CreateContext();

        var savedConversation = await verificationContext.Conversations
            .AsNoTracking()
            .SingleAsync(item => item.Id == conversation.Id);

        var savedMessages = await verificationContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversation.Id)
            .ToListAsync();

        Assert.Equal(
            "Quero criar uma API em C#.",
            savedConversation.Title
        );

        Assert.Equal(2, savedMessages.Count);
    }

    [Fact]
    public async Task AddMessagesAsync_ShouldRollbackAndKeepContextUsable_WhenSaveFails()
    {
        // Arrange
        await using var initialContext = _fixture.CreateContext();

        var initialRepository = new ConversationRepository(initialContext);

        var conversation = await initialRepository.CreateAsync();

        var conversationId = conversation.Id;

        var interceptor = new FailingSaveChangesInterceptor();

        await using var context = _fixture.CreateContext(interceptor);

        var repository = new ConversationRepository(context);

        var messages = new List<ChatMessage>
        {
            new(conversationId, "user", "Mensagem de teste."),
            new(conversationId, "assistant", "Resposta de teste.")
        };

        interceptor.ShouldFail = true;

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.AddMessagesAsync(
                messages,
                conversationTitle: "Título que não deve ser salvo."
            )
        );

        // Assert: verifica se o banco permaneceu consistente.
        var savedConversation = await context.Conversations
            .AsNoTracking()
            .SingleAsync(item => item.Id == conversationId);

        var savedMessages = await context.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .ToListAsync();

        Assert.Equal("Nova conversa", savedConversation.Title);
        Assert.Empty(savedMessages);

        // Verifica se as mensagens foram desanexadas do contexto.
        foreach (var message in messages)
        {
            Assert.Equal(
                EntityState.Detached,
                context.Entry(message).State
            );
        }

        // Act: tenta novamente usando o mesmo DbContext.
        interceptor.ShouldFail = false;

        await repository.AddMessagesAsync(
            messages,
            conversationTitle: "Título recuperado."
        );

        // Assert: confirma que o contexto continua funcional.
        var updatedConversation = await context.Conversations
            .AsNoTracking()
            .SingleAsync(item => item.Id == conversationId);

        var updatedMessages = await context.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .ToListAsync();

        Assert.Equal("Título recuperado.", updatedConversation.Title);
        Assert.Equal(2, updatedMessages.Count);
    }
}

public sealed class PostgreSqlTestFixture : IAsyncLifetime
{
    private readonly string _connectionString;

    public PostgreSqlTestFixture()
    {
        _connectionString =
            Environment.GetEnvironmentVariable(
                "ZY_POSTGRES_TEST_CONNECTION_STRING"
            )
            ?? throw new InvalidOperationException(
                "A variável ZY_POSTGRES_TEST_CONNECTION_STRING não foi configurada."
            );
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();

        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public ZYDbContext CreateContext(
        SaveChangesInterceptor? interceptor = null)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<ZYDbContext>()
                .UseNpgsql(_connectionString);

        if (interceptor is not null)
            optionsBuilder.AddInterceptors(interceptor);

        return new ZYDbContext(optionsBuilder.Options);
    }
}

public sealed class FailingSaveChangesInterceptor : SaveChangesInterceptor
{
    public bool ShouldFail { get; set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (ShouldFail)
        {
            return ValueTask.FromException<InterceptionResult<int>>(
                new InvalidOperationException(
                    "Falha simulada durante a persistência."
                )
            );
        }

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken
        );
    }
}
