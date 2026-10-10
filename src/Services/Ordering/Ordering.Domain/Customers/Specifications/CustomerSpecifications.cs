using BuildingBlocks.Abstractions.Specifications;

namespace Ordering.Domain.Customers.Specifications;

/// <summary>
/// Müşteriyi ID'ye göre getirir.
/// </summary>
public sealed class CustomerByIdSpec : Specification<Customer>
{
    public CustomerByIdSpec(Guid customerId) : base(c => c.Id == customerId)
    {
    }
}

/// <summary>
/// Müşteriyi müşteri koduna (code) göre getirir.
/// </summary>
public sealed class CustomerByCodeSpec : Specification<Customer>
{
    public CustomerByCodeSpec(string code) : base(c => c.Code == code)
    {
      ApplyNoTracking(); // performanslı bir sorgu
    }
}

/// <summary>
/// Müşteriyi email'e göre getirir.
/// </summary>
public sealed class CustomerByEmailSpec : Specification<Customer>
{
    public CustomerByEmailSpec(string email) : base(c => c.Email == email.ToLower())
    {
    }
}

/// <summary>
/// Aktif müşterileri listeler.
/// </summary>
public sealed class ActiveCustomersSpec : Specification<Customer>
{
    public ActiveCustomersSpec() : base(c => c.IsActive)
    {
    }
}

/// <summary>
/// Pasif müşterileri listeler.
/// </summary>
public sealed class InactiveCustomersSpec : Specification<Customer>
{
    public InactiveCustomersSpec() : base(c => !c.IsActive)
    {
    }
}

/// <summary>
/// Müşteri koduna göre aktif müşteriyi bulur.
/// </summary>
public sealed class ActiveCustomerByCodeSpec : Specification<Customer>
{
    public ActiveCustomerByCodeSpec(string code) : base(c => c.Code == code && c.IsActive)
    {
    }
}

/// <summary>
/// Tüm müşterileri alır (sayfalama ile).
/// </summary>
public sealed class AllCustomersSpec : Specification<Customer>
{
    public AllCustomersSpec(int pageNumber = 1, int pageSize = 10)
        : base()
    {
        //AddPaging((pageNumber - 1) * pageSize, pageSize);
    }
}
