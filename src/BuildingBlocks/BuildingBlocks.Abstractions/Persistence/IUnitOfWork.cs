namespace BuildingBlocks.Abstractions.Persistence;

/// <summary>
/// Unit of Work Pattern (driven port). Bir iş süreci boyunca yapılan değişiklikleri
/// tek bir atomik işlemle kalıcı hale getirir. Application katmanı EF Core'u bilmez; yalnızca bu sözleşmeyi bilir.
/// </summary>
public interface IUnitOfWork
{
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Değişiklikleri kaydeder ve biriken domain event'leri aynı transaction içinde dispatch eder.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction başlatır. Bilinçli olarak SENKRON tanımlanmıştır (bkz. UnitOfWork implementasyonu).
    /// </summary>
    ITransactionScope BeginTransaction();
}

public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
