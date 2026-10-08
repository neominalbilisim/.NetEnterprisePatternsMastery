namespace BuildingBlocks.Infrastructure.Persistence.Idempotency;

/// <summary>Idempotency-Key ile işlenmiş istek kaydı. Yanıt değeri jsonb olarak saklanır.</summary>
public sealed class IdempotencyRecord
{
    public Guid Key { get; init; }
    public string RequestName { get; init; } = default!;
    public string? ResponseJson { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
