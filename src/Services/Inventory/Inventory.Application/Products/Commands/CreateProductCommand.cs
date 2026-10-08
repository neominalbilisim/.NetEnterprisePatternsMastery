using BuildingBlocks.Abstractions.Idempotency;
using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Abstractions.Results;
using FluentValidation;
using Inventory.Domain.Products;
using Inventory.Domain.Specifications;

namespace Inventory.Application.Products.Commands;

/// <summary>Ürün oluşturma (idempotent). Aynı Idempotency-Key ile ikinci istek aynı ürün Id'sini döner.</summary>
public sealed record CreateProductCommand(
    Guid IdempotencyKey,
    string Sku,
    string Name,
    decimal UnitPrice,
    int InitialStock) : ICommand<Guid>, IIdempotentCommand;

internal sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitPrice).GreaterThan(0);
        RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0);
    }
}

internal sealed class CreateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var sku = command.Sku.Trim().ToUpperInvariant();
        if (await productRepository.AnyAsync(new ProductBySkuSpec(sku), cancellationToken))
        {
            return ProductErrors.SkuAlreadyExists(sku);
        }

        var productResult = Product.Create(sku, command.Name, command.UnitPrice, command.InitialStock);
        if (productResult.IsFailure)
        {
            return productResult.Error;
        }

        productRepository.Add(productResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return productResult.Value.Id;
    }
}
