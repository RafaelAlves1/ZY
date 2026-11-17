
using Microsoft.EntityFrameworkCore;
using ZY.Core.Models;

namespace ZY.Infrastructure.Persistence.PostgreSQL.Context;

public sealed class ZYDbContext : DbContext
{
    public ZYDbContext(DbContextOptions<ZYDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");

            entity.HasKey(conversation => conversation.Id);

            entity.Property(conversation => conversation.CreatedAt)
                .IsRequired();

            entity.Property(conversation => conversation.UpdatedAt)
                .IsRequired();

            entity.HasMany(conversation => conversation.Messages)
                .WithOne(message => message.Conversation)
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");

            entity.HasKey(message => message.Id);

            entity.Property(message => message.Role)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(message => message.Content)
                .IsRequired();

            entity.Property(message => message.CreatedAt)
                .IsRequired();

            entity.HasIndex(message => new
            {
                message.ConversationId,
                message.CreatedAt
            });
        });
    }
}
