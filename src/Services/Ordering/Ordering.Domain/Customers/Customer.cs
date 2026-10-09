using BuildingBlocks.Abstractions.Domain;
using BuildingBlocks.Abstractions.Results;
using Ordering.Domain.Customers.Events;

namespace Ordering.Domain.Customers;

/// <summary>
/// Customer aggregate root. Müşteri bilgileri ve business kuralları burada korunur.
/// Setterlar private'tır; state yalnızca anlamlı domain metotlarıyla değişir.
/// </summary>
public sealed class Customer : AggregateRoot<Guid>
{
    private Customer()
    {
        // EF Core için
    }

    private Customer(Guid id, string code, string name, string email, string? phoneNumber, string? taxId)
        : base(id)
    {
        Code = code;
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        TaxId = taxId;
        IsActive = true;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string Email { get; private set; } = default!;

    public string? PhoneNumber { get; private set; }

    public string? TaxId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public DateTime? UpdatedOnUtc { get; private set; }

    /// <summary>
    /// Factory metot: Geçersiz bir Customer nesnesinin oluşmasını engeller.
    /// </summary>
    public static Result<Customer> Create(string code, string name, string email, string? phoneNumber = null, string? taxId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return CustomerErrors.CodeRequired;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return CustomerErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return CustomerErrors.EmailRequired;
        }

        // Email format validation
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address != email)
            {
                return CustomerErrors.InvalidEmail;
            }
        }
        catch
        {
            return CustomerErrors.InvalidEmail;
        }

        var customer = new Customer(
            Guid.NewGuid(),
            code.Trim(),
            name.Trim(),
            email.Trim().ToLowerInvariant(),
            phoneNumber?.Trim(),
            taxId?.Trim());

        customer.Raise(new CustomerCreatedDomainEvent(
            customer.Id,
            customer.Code,
            customer.Name,
            customer.Email));

        return customer;
    }

    /// <summary>
    /// Müşteri bilgilerini günceller.
    /// </summary>
    public Result Update(string name, string email, string? phoneNumber = null, string? taxId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CustomerErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return CustomerErrors.EmailRequired;
        }

        // Email format validation
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address != email)
            {
                return CustomerErrors.InvalidEmail;
            }
        }
        catch
        {
            return CustomerErrors.InvalidEmail;
        }

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber?.Trim();
        TaxId = taxId?.Trim();
        UpdatedOnUtc = DateTime.UtcNow;

        Raise(new CustomerUpdatedDomainEvent(Id, Name, Email));

        return Result.Success();
    }

    /// <summary>
    /// Müşteriyi pasif (inactive) hale getirir.
    /// </summary>
    public Result Archive()
    {
        if (!IsActive)
        {
            return CustomerErrors.CustomerNotActive;
        }

        IsActive = false;
        UpdatedOnUtc = DateTime.UtcNow;

        Raise(new CustomerArchivedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Müşteriyi aktif hale getirir.
    /// </summary>
    public Result Activate()
    {
        if (IsActive)
        {
            return CustomerErrors.CustomerAlreadyActive;
        }

        IsActive = true;
        UpdatedOnUtc = DateTime.UtcNow;

        Raise(new CustomerActivatedDomainEvent(Id));

        return Result.Success();
    }
}
