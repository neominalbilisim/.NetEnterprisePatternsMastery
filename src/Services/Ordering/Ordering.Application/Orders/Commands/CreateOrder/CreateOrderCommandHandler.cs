using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Abstractions.Results;
using Microsoft.Extensions.Logging;
using Ordering.Application.Errors;
using Ordering.Application.Ports;
using Ordering.Domain.Orders;
using Ordering.Domain.Orders.Specifications;

namespace Ordering.Application.Orders.Commands.CreateOrder;

/// <summary>
/// Use case: Sipariş oluşturma.
/// Handler yalnızca PORT'ları bilir (ICustomerCreditService, IInventoryService, IOrderRepository, IUnitOfWork);
/// HTTP, gRPC, EF Core veya CAP detaylarından habersizdir (Hexagonal / Clean Architecture).
/// </summary>
internal sealed class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ICustomerCreditService customerCreditService,
    IInventoryService inventoryService,
    ILogger<CreateOrderCommandHandler> logger) : ICommandHandler<CreateOrderCommand, Guid>
{
    private const int MaxPendingOrdersPerCustomer = 5;

    public async Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // Not (bilinçli trade-off): TransactionBehavior transaction'ı handler'dan önce açar; bu yüzden aşağıdaki
        // uzak çağrılar açık bir DB transaction'ı içinde yapılır. Çağrılar kısa timeout'larla sınırlandırılmıştır.
        // Yüksek yükte bu kontroller transaction dışına (ör. ayrı bir ön-kontrol adımına) taşınabilir.

        // 1) Senkron HTTP: Legacy ERP'den kredi bilgisi (Anti-Corruption Layer + Resilience arkasında)
        var creditResult = await customerCreditService.GetCreditInfoAsync(command.CustomerCode, cancellationToken);
        if (creditResult.IsFailure)
        {
            return creditResult.Error;
        }

        var credit = creditResult.Value;
        if (!credit.IsActive)
        {
            return CustomerErrors.Inactive(command.CustomerCode);
        }

        // 2) Specification ile iş kuralı: Müşterinin bekleyen sipariş sayısı sınırı (birleşik spec)
        var pendingOrdersSpec = new OrdersByCustomerSpec(command.CustomerCode)
            .And(new OrdersByStatusSpec(OrderStatus.Pending));

        var pendingCount = await orderRepository.CountAsync(pendingOrdersSpec, cancellationToken);
        if (pendingCount >= MaxPendingOrdersPerCustomer)
        {
            return OrderErrors.TooManyPendingOrders(MaxPendingOrdersPerCustomer);
        }

        // 3) Senkron gRPC: Stok ve güncel fiyat (fiyata istemciden gelen değerle güvenilmez)
        var availabilityResult = await inventoryService.CheckAvailabilityAsync(
            command.Items.Select(i => new ProductQuantity(i.ProductId, i.Quantity)).ToList(),
            cancellationToken);

        if (availabilityResult.IsFailure)
        {
            return availabilityResult.Error;
        }

        var availability = availabilityResult.Value;

        var missingProducts = availability.Where(a => !a.Exists).Select(a => a.ProductId).ToList();
        if (missingProducts.Count > 0)
        {
            return OrderErrors.ProductsNotFound(missingProducts);
        }

        var insufficientProducts = availability.Where(a => !a.IsAvailable).Select(a => a.ProductName).ToList();
        if (insufficientProducts.Count > 0)
        {
            return OrderErrors.InsufficientStock(insufficientProducts);
        }

        // 4) Aggregate oluşturma (domain kuralları Order.Create içinde)
        var lines = availability
            .Select(a => new OrderLine(a.ProductId, a.ProductName, a.UnitPrice, a.RequestedQuantity))
            .ToList();

        var orderResult = Order.Create(command.CustomerCode, lines);
        if (orderResult.IsFailure)
        {
            return orderResult.Error;
        }

        var order = orderResult.Value;

        // 5) Kredi limiti kontrolü (ACL'den gelen, bizim dilimize çevrilmiş veriyle)
        if (order.TotalAmount > credit.AvailableCredit)
        {
            return OrderErrors.CreditLimitExceeded(order.TotalAmount, credit.AvailableCredit);
        }

        // 6) Kalıcılaştırma: SaveChanges -> OrderCreatedDomainEvent -> Integration Event (Outbox, aynı transaction)
        orderRepository.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Order {OrderId} created for customer {CustomerCode} with total {TotalAmount}",
            order.Id, order.CustomerCode, order.TotalAmount);

        return order.Id;
    }
}
