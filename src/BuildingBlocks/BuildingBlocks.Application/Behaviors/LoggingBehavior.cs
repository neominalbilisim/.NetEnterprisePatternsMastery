using System.Diagnostics;
using BuildingBlocks.Abstractions.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Pipeline'ın en dış halkası: Her isteğin adını, süresini ve (varsa) hata kodunu yapısal olarak loglar.
/// Serilog scope'a eklenen RequestName alanı Seq üzerinde filtrelenebilir.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using (logger.BeginScope(new Dictionary<string, object> { ["RequestName"] = requestName }))
        {
            logger.LogInformation("Processing request {RequestName}", requestName);
            var stopwatch = Stopwatch.StartNew();

            var response = await next();

            stopwatch.Stop();
            if (response.IsSuccess)
            {
                logger.LogInformation(
                    "Request {RequestName} completed in {ElapsedMilliseconds} ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogWarning(
                    "Request {RequestName} failed with {ErrorCode}: {ErrorDescription} ({ElapsedMilliseconds} ms)",
                    requestName, response.Error.Code, response.Error.Description, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
    }
}
