using BuildingBlocks.Abstractions.Specifications;
using BuildingBlocks.Infrastructure.Persistence;
using Ordering.Domain.Customers;

namespace Ordering.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(OrderingDbContext context)
    : EfRepository<Customer, Guid>(context), ICustomerRepository
{

}