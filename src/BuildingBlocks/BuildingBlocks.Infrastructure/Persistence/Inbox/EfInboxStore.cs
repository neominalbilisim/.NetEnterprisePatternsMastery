using BuildingBlocks.Abstractions.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Inbox;

internal sealed class EfInboxStore<TContext>(TContext context) : IInboxStore
    where TContext : DbContext
{
    public Task<bool> IsProcessedAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default) =>
        context.Set<InboxMessage>()
            .AnyAsync(m => m.MessageId == messageId && m.Consumer == consumerName, cancellationToken);

    public Task AddProcessedAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default)
    {
        context.Set<InboxMessage>().Add(new InboxMessage
        {
            MessageId = messageId,
            Consumer = consumerName,
            ProcessedOnUtc = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }
}
