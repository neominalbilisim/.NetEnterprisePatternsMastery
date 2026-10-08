using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Results;
using FluentValidation;
using Ordering.Application.Ports;

namespace Ordering.Application.Orders.Queries;

/// <summary>
/// HTTP -> gRPC örneği: İstemci Ordering'e REST ile gelir, Ordering Inventory'ye gRPC ile sorar.
/// </summary>
public sealed record CheckProductAvailabilityQuery(IReadOnlyList<ProductQuantity> Items)
    : IQuery<IReadOnlyList<ProductAvailability>>;

internal sealed class CheckProductAvailabilityQueryValidator : AbstractValidator<CheckProductAvailabilityQuery>
{
    public CheckProductAvailabilityQueryValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

internal sealed class CheckProductAvailabilityQueryHandler(IInventoryService inventoryService)
    : IQueryHandler<CheckProductAvailabilityQuery, IReadOnlyList<ProductAvailability>>
{
    public Task<Result<IReadOnlyList<ProductAvailability>>> Handle(
        CheckProductAvailabilityQuery query, CancellationToken cancellationToken) =>
        inventoryService.CheckAvailabilityAsync(query.Items, cancellationToken);
}
