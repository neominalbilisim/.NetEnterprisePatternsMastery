using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Abstractions.Results;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Customers;
using Ordering.Domain.Customers.Specifications;

namespace Ordering.Application.Customers.Commands.CreateCustomer;

/// <summary>
/// Use case: Müşteri oluşturma.
/// Handler yalnızca ICustomerRepository ve IUnitOfWork'ü bilir.
/// HTTP, EF Core veya CAP detaylarından habersizdir (Hexagonal / Clean Architecture).
/// 
/// NOT: Müşteri oluşturma sırasında ERP/gRPC servislere istek atılmaz.
/// Domain Events yayınlanarak diğer operasyonlar tetiklenir.
/// </summary>
internal sealed class CreateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateCustomerCommandHandler> logger) : ICommandHandler<CreateCustomerCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        // 1) Müşteri kodu benzersizlik kontrolü (Specification Pattern kullanımı)
        var customerByCodeSpec = new CustomerByCodeSpec(command.Code);
    //var customerByCodeOrcustomerByEmailSpec = customerByCodeSpec.And(new CustomerByEmailSpec(command.Email));

    



    var existingCustomerByCode = await customerRepository.FirstOrDefaultAsync(customerByCodeSpec, cancellationToken);
  
    // FirstOrDefaultAsync çağırdığımız SpecEvulator GetQuery Methodu ile Lamda Expression spec içerisindeki bilgileri çekerek LINQ sorgusuna çeviriyoruz. 

    if (existingCustomerByCode != null)
        {
            return CustomerErrors.DuplicateCode(command.Code);
        }

        // 2) Email benzersizlik kontrolü (Specification Pattern kullanımı)
        var customerByEmailSpec = new CustomerByEmailSpec(command.Email);
        var existingCustomerByEmail = await customerRepository.FirstOrDefaultAsync(customerByEmailSpec, cancellationToken);

        if (existingCustomerByEmail != null)
        {
            return CustomerErrors.DuplicateEmail(command.Email);
        }

        // 3) Domain aggregate oluşturma (domain kuralları Customer.Create içinde)
        var customerResult = Customer.Create(
            command.Code,
            command.Name,
            command.Email,
            command.PhoneNumber,
            command.TaxId);

        if (customerResult.IsFailure)
        {
            return customerResult.Error;
        }

        var customer = customerResult.Value;

        // 4) Kalıcılaştırma: SaveChanges -> CustomerCreatedDomainEvent -> Event Handlers (Outbox Pattern)
        customerRepository.Add(customer);
        //await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Customer {CustomerId} created with code {Code} and name {Name}",
            customer.Id, customer.Code, customer.Name);

        return customer.Id;
    }
}
