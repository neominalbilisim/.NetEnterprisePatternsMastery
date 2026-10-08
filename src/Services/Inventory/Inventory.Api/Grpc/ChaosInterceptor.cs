using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Inventory.Api.Grpc;

/// <summary>
/// Chaos engineering amaçlı gRPC interceptor. "Chaos:GrpcFailureRate" oranında çağrıyı StatusCode.Unavailable ile
/// reddeder. Ordering tarafındaki gRPC retry politikası bu hataları otomatik olarak yeniden dener (Seq loglarında izlenebilir).
/// </summary>
public sealed class ChaosInterceptor(IConfiguration configuration, ILogger<ChaosInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var failureRate = configuration.GetValue<double>("Chaos:GrpcFailureRate");

        if (failureRate > 0 && Random.Shared.NextDouble() < failureRate)
        {
            logger.LogWarning("Chaos: simulating gRPC outage for {Method}", context.Method);
            throw new RpcException(new Status(StatusCode.Unavailable, "Chaos: simulated transient outage."));
        }

        return await continuation(request, context);
    }
}
