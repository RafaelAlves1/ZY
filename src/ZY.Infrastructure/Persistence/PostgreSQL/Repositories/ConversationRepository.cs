
using Microsoft.EntityFrameworkCore;
using ZY.Core.Interfaces;
using ZY.Core.Models;
using ZY.Infrastructure.Persistence.PostgreSQL.Context;

namespace ZY.Infrastructure.Persistence.PostgreSQL.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly ZYDbContext _context;

    public ConversationRepository(ZYDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(
                conversation => conversation.Id == conversationId,
                cancellationToken
            );
    }

    public async Task<IReadOnlyList<Conversation>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .AsNoTracking()
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Conversation?> GetMostRecentAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Conversation> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation();

        await _context.Conversations.AddAsync(
            conversation,
            cancellationToken
        );

        await _context.SaveChangesAsync(cancellationToken);

        return conversation;
    }

    public async Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(
        Guid conversationId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            return [];

        var messages = await _context.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAt)
            .ThenByDescending(message => message.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        messages.Reverse();

        return messages;
    }

    public async Task AddMessageAsync(
        ChatMessage message,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(
                item => item.Id == message.ConversationId,
                cancellationToken
            );

        if (conversation is null)
        {
            throw new InvalidOperationException(
                "A conversa associada à mensagem não foi encontrada."
            );
        }

        conversation.UpdateActivity();

        await _context.Messages.AddAsync(message, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        await _context.Messages
            .Where(message => message.ConversationId == conversationId)
            .ExecuteDeleteAsync(cancellationToken);

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(
                item => item.Id == conversationId,
                cancellationToken
            );

        if (conversation is not null)
        {
            conversation.UpdateActivity();

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateConversationAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        _context.Conversations.Update(conversation);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMessagesAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default,
        string? conversationTitle = null)
    {
        if (messages.Count == 0)
            return;

        var conversationId = messages[0].ConversationId;

        if (messages.Any(message => message.ConversationId != conversationId))
        {
            throw new InvalidOperationException(
                "Todas as mensagens devem pertencer à mesma conversa."
            );
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        Conversation? conversation = null;

        try
        {
            conversation = await _context.Conversations
                .FirstOrDefaultAsync(
                    item => item.Id == conversationId,
                    cancellationToken
                );

            if (conversation is null)
            {
                throw new InvalidOperationException(
                    "A conversa associada às mensagens não foi encontrada."
                );
            }

            conversation.UpdateActivity();

            if (!string.IsNullOrWhiteSpace(conversationTitle))
                conversation.UpdateTitle(conversationTitle);

            await _context.Messages.AddRangeAsync(
                messages,
                cancellationToken
            );

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);

            if (conversation is not null)
            {
                await _context.Entry(conversation)
                    .ReloadAsync(CancellationToken.None);
            }

            foreach (var message in messages)
            {
                _context.Entry(message).State = EntityState.Detached;
            }

            throw;
        }
    }
}
