using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Abstractions.Specifications;
using Ordering.Domain.Orders;

namespace Ordering.Domain.Customers;

public interface ICustomerRepository: IRepository<Customer, Guid>
{}
