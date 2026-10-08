using BuildingBlocks.Abstractions.Idempotency;
using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Abstractions.Results;
using BuildingBlocks.Application.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Inbox Pattern (idempotent consumer). Outbox "en az bir kez" teslim garantisi verir; aynı mesaj
/// birden fazla gelebilir. Bu behavior işlenen mesaj kimliğini iş verisiyle aynı transaction'da saklar
/// ve tekrar gelen mesajı handler'a hiç ulaştırmadan başarılı kabul eder.
/// </summary>
public sealed class InboxBehavior<TRequest, TResponse>(
    IInboxStore inboxStore,
    IUnitOfWork unitOfWork,
    ILogger<InboxBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IInboxCommand
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (await inboxStore.IsProcessedAsync(request.MessageId, request.ConsumerName, cancellationToken))
        {
            logger.LogInformation(
                "Message {MessageId} was already processed by {ConsumerName}; skipping",
                request.MessageId, request.ConsumerName);
            return ResultFactory.SuccessFromJson<TResponse>(null);
        }

        var response = await next();

        if (response.IsSuccess)
        {
            await inboxStore.AddProcessedAsync(request.MessageId, request.ConsumerName, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
