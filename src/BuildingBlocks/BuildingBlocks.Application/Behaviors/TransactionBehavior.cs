using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Abstractions.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Yalnızca command'lar için çalışır (IBaseCommand). Handler'ı tek bir transaction içine alır:
/// - İş verisi (EF Core),
/// - Outbox kayıtları (CAP -> cap.published),
/// - Inbox / Idempotency kayıtları
/// hepsi aynı transaction'da commit edilir ya da birlikte geri alınır.
/// Validation bu behavior'dan ÖNCE çalışır; böylece geçersiz istekler için transaction açılmaz.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // İç içe command çağrılarında dıştaki transaction kullanılır.
        if (unitOfWork.HasActiveTransaction)
        {
            return await next();
        }

        // BeginTransaction senkron çağrılır: CAP publisher'ın transaction bağlamı (AsyncLocal)
        // bu metodun devamındaki tüm çağrılara (handler, domain event handler'lar) akar.
        await using var transaction = unitOfWork.BeginTransaction();

        var response = await next();

        if (response.IsSuccess)
        {
            await transaction.CommitAsync(cancellationToken);
            logger.LogDebug("Transaction committed for {RequestName}", typeof(TRequest).Name);
        }
        else
        {
            // Beklenen iş hatası: hiçbir şey kalıcı olmaz (outbox mesajları dahil).
            await transaction.RollbackAsync(cancellationToken);
            logger.LogDebug("Transaction rolled back for {RequestName}", typeof(TRequest).Name);
        }

        // Exception durumunda commit edilmemiş transaction DisposeAsync ile otomatik geri alınır.
        return response;
    }
}
