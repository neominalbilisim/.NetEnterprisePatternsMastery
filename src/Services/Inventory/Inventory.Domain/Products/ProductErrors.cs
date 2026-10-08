using BuildingBlocks.Abstractions.Results;

namespace Inventory.Domain.Products;

public static class ProductErrors
{
    public static readonly Error SkuRequired = Error.Validation("Product.SkuRequired", "SKU is required.");
    public static readonly Error NameRequired = Error.Validation("Product.NameRequired", "Product name is required.");
    public static readonly Error InvalidPrice = Error.Validation("Product.InvalidPrice", "Unit price must be greater than zero.");
    public static readonly Error InvalidQuantity = Error.Validation("Product.InvalidQuantity", "Quantity is invalid.");

    public static Error NotFound(Guid productId) =>
        Error.NotFound("Product.NotFound", $"Product '{productId}' was not found.");

    public static Error SkuAlreadyExists(string sku) =>
        Error.Conflict("Product.SkuAlreadyExists", $"A product with SKU '{sku}' already exists.");

    public static Error InsufficientStock(string sku, int requested, int available) =>
        Error.Conflict("Product.InsufficientStock",
            $"Insufficient stock for '{sku}': requested {requested}, available {available}.");
}
