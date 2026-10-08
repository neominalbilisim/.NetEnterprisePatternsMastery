namespace BuildingBlocks.Abstractions.Idempotency;

/// <summary>
/// İşlenmiş mesajların kaydını tutar (driven port). Kayıt, iş verisiyle AYNI transaction içinde yazılır.
/// </summary>
public interface IInboxStore
{
    Task<bool> IsProcessedAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default);

    /// <summary>Kaydı ekler; kalıcı hale gelmesi Unit of Work ile olur.</summary>
    Task AddProcessedAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Idempotency-Key ile işlenmiş istekleri ve yanıtlarını saklar (driven port).
/// </summary>
public interface IIdempotencyStore
{
    Task<IdempotentResponse?> GetAsync(Guid key, string requestName, CancellationToken cancellationToken = default);

    /// <summary>Kaydı ekler; kalıcı hale gelmesi Unit of Work ile olur.</summary>
    Task AddAsync(Guid key, string requestName, string? responseJson, CancellationToken cancellationToken = default);
}

public sealed record IdempotentResponse(Guid Key, string RequestName, string? ResponseJson);
