using BuildingBlocks.Abstractions.Results;
using Contracts.Grpc.Inventory;
using Grpc.Core;
using Inventory.Application.Products.Queries;
using MediatR;

namespace Inventory.Api.Grpc;

/// <summary>
/// Driving adapter (gRPC). Protobuf mesajlarını query'lere çevirir; Result hatalarını gRPC StatusCode'larına eşler.
/// REST endpoint'leriyle AYNI use case'leri kullanır: iletişim protokolü değişir, iş mantığı değişmez.
/// </summary>
public sealed class InventoryGrpcService(ISender sender) : InventoryService.InventoryServiceBase
{
    public override async Task<CheckAvailabilityReply> CheckAvailability(
        CheckAvailabilityRequest request, ServerCallContext context)
    {
        var items = new List<StockCheckItem>(request.Items.Count);
        foreach (var item in request.Items)
        {
            if (!Guid.TryParse(item.ProductId, out var productId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid product id '{item.ProductId}'."));
            }

            items.Add(new StockCheckItem(productId, item.Quantity));
        }

        var result = await sender.Send(new CheckStockAvailabilityQuery(items), context.CancellationToken);
        if (result.IsFailure)
        {
            throw ToRpcException(result.Error);
        }

        var reply = new CheckAvailabilityReply { AllAvailable = result.Value.All(x => x.IsAvailable) };
        reply.Items.AddRange(result.Value.Select(x => new StockItemAvailability
        {
            ProductId = x.ProductId.ToString(),
            Exists = x.Exists,
            ProductName = x.ProductName,
            UnitPrice = x.UnitPrice,
            RequestedQuantity = x.RequestedQuantity,
            AvailableQuantity = x.AvailableQuantity,
            IsAvailable = x.IsAvailable
        }));

        return reply;
    }

    public override async Task<ProductReply> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ProductId, out var productId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid product id '{request.ProductId}'."));
        }

        var result = await sender.Send(new GetProductByIdQuery(productId), context.CancellationToken);
        if (result.IsFailure)
        {
            throw ToRpcException(result.Error);
        }

        var product = result.Value;
        return new ProductReply
        {
            ProductId = product.Id.ToString(),
            Sku = product.Sku,
            Name = product.Name,
            UnitPrice = product.UnitPrice,
            AvailableQuantity = product.AvailableQuantity
        };
    }

    private static RpcException ToRpcException(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCode.InvalidArgument,
            ErrorType.NotFound => StatusCode.NotFound,
            ErrorType.Conflict => StatusCode.FailedPrecondition,
            ErrorType.Unavailable => StatusCode.Unavailable,
            _ => StatusCode.FailedPrecondition
        };

        var metadata = new Metadata { { "error-code", error.Code } };
        return new RpcException(new Status(statusCode, error.Description), metadata);
    }
}
