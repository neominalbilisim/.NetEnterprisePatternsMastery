using BuildingBlocks.Abstractions.Results;

namespace Ordering.Application.Ports;

/// <summary>
/// Driven port: Müşteri kredi bilgisi. Gerçekte Legacy ERP'den gelir; ancak uygulama katmanı
/// ERP'nin modelini (CUST_NO, CRD_LMT...) hiç bilmez. Çeviri Anti-Corruption Layer'da yapılır.
/// </summary>
public interface ICustomerCreditService
{
    Task<Result<CustomerCreditInfo>> GetCreditInfoAsync(string customerCode, CancellationToken cancellationToken = default);
}

/// <summary>Bizim bounded context'imizin diliyle müşteri kredi bilgisi.</summary>
public sealed record CustomerCreditInfo(
    string CustomerCode,
    string FullName,
    bool IsActive,
    decimal AvailableCredit,
    string Currency);
