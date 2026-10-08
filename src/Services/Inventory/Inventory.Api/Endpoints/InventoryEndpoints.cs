using BuildingBlocks.Infrastructure.Web;
using Inventory.Application.Products.Commands;
using Inventory.Application.Products.Queries;
using Inventory.Application.Reservations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products").WithTags("Products");

        products.MapGet("/", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetProductsQuery(), ct)).Match(value => Results.Ok(value)));

        products.MapGet("/{productId:guid}", async (Guid productId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetProductByIdQuery(productId), ct)).Match(value => Results.Ok(value)))
            .WithName("GetProductById");

        products.MapPost("/", async (
            [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
            CreateProductRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            if (idempotencyKey is null || idempotencyKey == Guid.Empty)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Missing idempotency key",
                    detail: "The 'Idempotency-Key' header is required and must be a valid GUID.");
            }

            var result = await sender.Send(new CreateProductCommand(
                idempotencyKey.Value, request.Sku, request.Name, request.UnitPrice, request.InitialStock), ct);

            return result.Match(productId =>
                Results.CreatedAtRoute("GetProductById", new { productId }, new { productId }));
        });

        app.MapGet("/api/reservations/by-order/{orderId:guid}", async (Guid orderId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetReservationByOrderIdQuery(orderId), ct)).Match(value => Results.Ok(value)))
            .WithTags("Reservations");

        return app;
    }
}

public sealed record CreateProductRequest(string Sku, string Name, decimal UnitPrice, int InitialStock);
