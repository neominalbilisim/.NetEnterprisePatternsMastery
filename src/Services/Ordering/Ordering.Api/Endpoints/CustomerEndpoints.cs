using BuildingBlocks.Infrastructure.Web;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Application.Customers.Commands.CreateCustomer;
using Ordering.Domain.Customers;

namespace Ordering.Api.Endpoints;

/// <summary>
/// Driving adapter (HTTP). Customer endpoint'leri.
/// HTTP isteğini command'a çevirir, MediatR'a gönderir, Result'ı HTTP yanıtına dönüştürür.
/// </summary>
public static class CustomerEndpoints
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");

        group.MapPost("/", CreateCustomerAsync).WithName("CreateCustomer");
        group.MapGet("/{customerId:guid}", GetCustomerByIdAsync).WithName("GetCustomerById");

        return app;
    }

    private static async Task<IResult> CreateCustomerAsync(
        CreateCustomerRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        

        var result = await sender.Send(
            new CreateCustomerCommand(
                Guid.NewGuid(),
                request.Code,
                request.Name,
                request.Email,
                request.PhoneNumber,
                request.TaxId),
            cancellationToken);

        return result.Match(customerId =>
            Results.Created($"/api/customers/{customerId}", new { CustomerId = customerId }));
    }

    private static async Task<IResult> GetCustomerByIdAsync(
        Guid customerId,
        ICustomerRepository customerRepository,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken);

        if (customer is null)
        {
            return Results.NotFound(new { message = $"Customer '{customerId}' was not found." });
        }

        return Results.Ok(new CustomerResponse(
            customer.Id,
            customer.Code,
            customer.Name,
            customer.Email,
            customer.PhoneNumber,
            customer.TaxId,
            customer.IsActive,
            customer.CreatedOnUtc));
    }
}

/// <summary>
/// Customer oluşturma isteği tarafından gelen payload.
/// </summary>
public sealed record CreateCustomerRequest(
    string Code,
    string Name,
    string Email,
    string? PhoneNumber = null,
    string? TaxId = null);

/// <summary>
/// Customer response modeli.
/// </summary>
public sealed record CustomerResponse(
    Guid Id,
    string Code,
    string Name,
    string Email,
    string? PhoneNumber,
    string? TaxId,
    bool IsActive,
    DateTime CreatedOnUtc);
