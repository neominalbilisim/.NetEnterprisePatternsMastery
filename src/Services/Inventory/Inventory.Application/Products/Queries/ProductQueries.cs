using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Results;
using FluentValidation;
using Inventory.Application.Ports;
using Inventory.Domain.Products;

namespace Inventory.Application.Products.Queries;

// ---------- Tek ürün ----------
public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductResponse>;

internal sealed class GetProductByIdQueryHandler(IInventoryReadRepository readRepository)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await readRepository.GetProductByIdAsync(query.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(query.ProductId);
        }

        return product;
    }
}

// ---------- Ürün listesi ----------
public sealed record GetProductsQuery : IQuery<IReadOnlyList<ProductResponse>>;

internal sealed class GetProductsQueryHandler(IInventoryReadRepository readRepository)
    : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductResponse>>
{
    public async Task<Result<IReadOnlyList<ProductResponse>>> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        var products = await readRepository.GetProductsAsync(cancellationToken);
        return Result.Success(products);
    }
}

// ---------- Stok uygunluk kontrolü (gRPC servisi tarafından kullanılır) ----------
public sealed record StockCheckItem(Guid ProductId, int Quantity);

public sealed record StockAvailabilityResponse(
    Guid ProductId,
    bool Exists,
    string ProductName,
    decimal UnitPrice,
    int RequestedQuantity,
    int AvailableQuantity,
    bool IsAvailable);

public sealed record CheckStockAvailabilityQuery(IReadOnlyList<StockCheckItem> Items)
    : IQuery<IReadOnlyList<StockAvailabilityResponse>>;

internal sealed class CheckStockAvailabilityQueryValidator : AbstractValidator<CheckStockAvailabilityQuery>
{
    public CheckStockAvailabilityQueryValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

internal sealed class CheckStockAvailabilityQueryHandler(IInventoryReadRepository readRepository)
    : IQueryHandler<CheckStockAvailabilityQuery, IReadOnlyList<StockAvailabilityResponse>>
{
    public async Task<Result<IReadOnlyList<StockAvailabilityResponse>>> Handle(
        CheckStockAvailabilityQuery query, CancellationToken cancellationToken)
    {
        var productIds = query.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = (await readRepository.GetProductsByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        IReadOnlyList<StockAvailabilityResponse> response = query.Items
            .Select(item => products.TryGetValue(item.ProductId, out var product)
                ? new StockAvailabilityResponse(
                    product.Id, true, product.Name, product.UnitPrice,
                    item.Quantity, product.AvailableQuantity, product.AvailableQuantity >= item.Quantity)
                : new StockAvailabilityResponse(
                    item.ProductId, false, string.Empty, 0m, item.Quantity, 0, false))
            .ToList();

        return Result.Success(response);
    }
}
