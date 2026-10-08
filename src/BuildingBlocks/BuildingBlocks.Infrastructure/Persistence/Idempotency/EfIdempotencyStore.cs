using BuildingBlocks.Abstractions.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Idempotency;

internal sealed class EfIdempotencyStore<TContext>(TContext context) : IIdempotencyStore
    where TContext : DbContext
{
    public async Task<IdempotentResponse?> GetAsync(Guid key, string requestName, CancellationToken cancellationToken = default)
    {
        var record = await context.Set<IdempotencyRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key && r.RequestName == requestName, cancellationToken);

        return record is null ? null : new IdempotentResponse(record.Key, record.RequestName, record.ResponseJson);
    }

    public Task AddAsync(Guid key, string requestName, string? responseJson, CancellationToken cancellationToken = default)
    {
        context.Set<IdempotencyRecord>().Add(new IdempotencyRecord
        {
            Key = key,
            RequestName = requestName,
            ResponseJson = responseJson,
            CreatedOnUtc = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }
}
