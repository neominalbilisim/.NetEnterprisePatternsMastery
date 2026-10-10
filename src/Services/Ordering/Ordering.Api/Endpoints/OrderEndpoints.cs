using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Web;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Application.Orders.Commands.CreateOrder;
using Ordering.Application.Orders.Queries;
using Ordering.Application.Ports;

namespace Ordering.Api.Endpoints;

/// <summary>
/// Driving adapter (HTTP). Endpoint'ler ince tutulur: HTTP isteğini command/query'ye çevirir,
/// MediatR'a gönderir, Result'ı HTTP yanıtına dönüştürür. İş mantığı içermez.
/// </summary>
public static class OrderEndpoints
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapPost("/", CreateOrderAsync).WithName("CreateOrder");
        group.MapGet("/{orderId:guid}", GetOrderByIdAsync).WithName("GetOrderById");
        group.MapGet("/", GetCustomerOrdersAsync).WithName("GetCustomerOrders");
        group.MapPost("/availability", CheckAvailabilityAsync).WithName("CheckAvailability");

        return app;
    }

    private static async Task<IResult> CreateOrderAsync(
        [FromHeader(Name = IdempotencyKeyHeader)] Guid? idempotencyKey,
        CreateOrderRequest request,
        ISender sender,
        CancellationToken cancellationToken
        )
    {
        if (idempotencyKey is null || idempotencyKey == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Missing idempotency key",
                detail: $"The '{IdempotencyKeyHeader}' header is required and must be a valid GUID.",
                extensions: new Dictionary<string, object?> { ["errorCode"] = "Request.IdempotencyKeyMissing" });
        }

        var items = request.Items?.Select(i => new CreateOrderItem(i.ProductId, i.Quantity)).ToList()
                    ?? new List<CreateOrderItem>();


   

    var result = await sender.Send(
            new CreateOrderCommand(idempotencyKey.Value, request.CustomerCode, items), cancellationToken);


    // transaction commit olduğuna eminiz.

    //if(result.IsSuccess)
    //{
    //  var queryResult = await sender.Send(new GetCustomerOrdersQuery(request.CustomerCode));
    //}



    return result.Match(orderId =>
            Results.Created($"/api/orders/{orderId}", new { OrderId = orderId }));
  }

    private static async Task<IResult> GetOrderByIdAsync(Guid orderId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrderByIdQuery(orderId), cancellationToken);
        return result.Match(value => Results.Ok(value));
    }

    private static async Task<IResult> GetCustomerOrdersAsync(
        [FromQuery] string customerCode,
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await sender.Send(new GetCustomerOrdersQuery(customerCode, page, pageSize), cancellationToken);
        return result.Match(value => Results.Ok(value));
    }

    /// <summary>HTTP (istemci -> Ordering) + gRPC (Ordering -> Inventory) senkron zincir örneği.</summary>
    private static async Task<IResult> CheckAvailabilityAsync(
        AvailabilityRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var items = request.Items?.Select(i => new ProductQuantity(i.ProductId, i.Quantity)).ToList()
                    ?? new List<ProductQuantity>();

        var result = await sender.Send(new CheckProductAvailabilityQuery(items), cancellationToken);
        return result.Match(value => Results.Ok(value));
    }
}

public sealed record CreateOrderRequest(string CustomerCode, IReadOnlyList<OrderItemRequest>? Items);

public sealed record OrderItemRequest(Guid ProductId, int Quantity);

public sealed record AvailabilityRequest(IReadOnlyList<OrderItemRequest>? Items);
