using BuildingBlocks.Abstractions.Idempotency;
using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Abstractions.Results;
using BuildingBlocks.Application.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// API seviyesinde idempotency (Idempotency-Key). Aynı anahtarla gelen tekrar isteklerde handler
/// yeniden çalıştırılmaz; ilk başarılı yanıt döndürülür.
/// TransactionBehavior'ın İÇİNDE çalışır: kayıt, iş verisiyle aynı transaction'da yazılır.
/// Eşzamanlı aynı anahtarlı isteklerde (race condition) veritabanındaki birincil anahtar kısıtı ikinci kaydı engeller.
/// </summary>
public sealed class IdempotencyBehavior<TRequest, TResponse>(
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        var existing = await idempotencyStore.GetAsync(request.IdempotencyKey, requestName, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation(
                "Duplicate request {RequestName} detected for idempotency key {IdempotencyKey}; returning stored response",
                requestName, request.IdempotencyKey);
            return ResultFactory.SuccessFromJson<TResponse>(existing.ResponseJson);
        }

        var response = await next();

        // Yalnızca başarılı sonuçlar saklanır; başarısız istek aynı anahtarla tekrar denenebilir.
        if (response.IsSuccess)
        {
            await idempotencyStore.AddAsync(
                request.IdempotencyKey, requestName, ResultFactory.SerializeValue(response), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
