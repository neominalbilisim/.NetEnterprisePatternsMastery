using System.Globalization;
using Ordering.Application.Ports;

namespace Ordering.Infrastructure.Adapters.LegacyErp;

/// <summary>
/// ANTI-CORRUPTION LAYER - Translator:
/// Legacy ERP'nin modelini ve kavramlarını bizim bounded context'imizin diline çevirir.
/// ERP modelinde bir değişiklik olduğunda yalnızca bu sınıf etkilenir; domain ve use case'ler korunur.
/// </summary>
internal static class LegacyErpCustomerTranslator
{
    // Kültür (tr-TR) yerine açık format: container'larda ICU/globalization ayarından bağımsız çalışır.
    private static readonly NumberFormatInfo ErpNumberFormat = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "."
    };

    public static CustomerCreditInfo ToCustomerCreditInfo(ErpCustomerResponse erpCustomer)
    {
        var creditLimit = ParseAmount(erpCustomer.CreditLimit);
        var creditUsed = ParseAmount(erpCustomer.CreditUsed);

        return new CustomerCreditInfo(
            CustomerCode: erpCustomer.CustomerNo.Trim(),
            FullName: ToTitle(erpCustomer.CustomerName),
            IsActive: IsActive(erpCustomer.StatusCode),
            AvailableCredit: Math.Max(0m, creditLimit - creditUsed),
            Currency: NormalizeCurrency(erpCustomer.CurrencyCode));
    }

    private static decimal ParseAmount(string? rawAmount) =>
        decimal.TryParse(rawAmount, NumberStyles.Number, ErpNumberFormat, out var amount) ? amount : 0m;

    // ERP'de yalnızca "A" aktif demektir; "P" (pasif) ve "B" (bloke) sipariş veremez.
    private static bool IsActive(string? statusCode) =>
        string.Equals(statusCode?.Trim(), "A", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeCurrency(string? currencyCode) =>
        currencyCode?.Trim().ToUpperInvariant() switch
        {
            null or "" or "TL" or "TRY" => "TRY",
            var other => other
        };

    // ERP isimleri büyük harf ve sağdan boşluklu tutuyor: "AHMET YILMAZ   " -> "Ahmet Yilmaz"
    private static string ToTitle(string? name) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase((name ?? string.Empty).Trim().ToLowerInvariant());
}
