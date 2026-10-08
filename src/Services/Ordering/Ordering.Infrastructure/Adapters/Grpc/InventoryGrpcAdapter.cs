using BuildingBlocks.Abstractions.Results;
using Contracts.Grpc.Inventory;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ordering.Application.Errors;
using Ordering.Application.Ports;

namespace Ordering.Infrastructure.Adapters.Grpc;

/// <summary>
/// IInventoryService portunun gRPC adapter'ı. Protobuf tipleri bu sınıfın dışına çıkmaz;
/// uygulama katmanına kendi modelimiz (ProductAvailability) döner.
/// Resiliency: Kanal seviyesinde gRPC retry policy (DI'da) + çağrı başına deadline.
/// </summary>
internal sealed class InventoryGrpcAdapter(
    InventoryService.InventoryServiceClient client,
    ILogger<InventoryGrpcAdapter> logger) : IInventoryService
{
    // Deadline tüm retry denemelerini kapsar; süre dolarsa çağrı DeadlineExceeded ile sonlanır.
    private static readonly TimeSpan CallDeadline = TimeSpan.FromSeconds(300);

    public async Task<Result<IReadOnlyList<ProductAvailability>>> CheckAvailabilityAsync(
        IReadOnlyCollection<ProductQuantity> items, CancellationToken cancellationToken = default)
    {
        var request = new CheckAvailabilityRequest();
        request.Items.AddRange(items.Select(i => new StockItemRequest
        {
            ProductId = i.ProductId.ToString(),
            Quantity = i.Quantity
        }));

        try
        {
            var reply = await client.CheckAvailabilityAsync(
                request,
                deadline: DateTime.UtcNow.Add(CallDeadline),
                cancellationToken: cancellationToken);

            IReadOnlyList<ProductAvailability> availability = reply.Items
                .Select(i => new ProductAvailability(
                    Guid.Parse(i.ProductId),
                    i.Exists,
                    i.ProductName,
                    i.UnitPrice is null ? 0m : (decimal)i.UnitPrice,
                    i.RequestedQuantity,
                    i.AvailableQuantity,
                    i.IsAvailable))
                .ToList();

            return Result.Success(availability);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            logger.LogWarning(ex, "Inventory gRPC service is unavailable ({StatusCode})", ex.StatusCode);
            return Result.Failure<IReadOnlyList<ProductAvailability>>(InventoryErrors.ServiceUnavailable);
        }
    }
}
