using System.Text.Json.Serialization;

namespace Ordering.Infrastructure.Adapters.LegacyErp;

/// <summary>
/// Legacy ERP'nin HAM modeli. Kısaltılmış kolon adları, metin olarak tutulan ve Türkçe formatlı tutarlar,
/// tek harfli durum kodları... Bu sınıf Anti-Corruption Layer'ın dışına ASLA çıkmaz.
/// </summary>
internal sealed class ErpCustomerResponse
{
    [JsonPropertyName("CUST_NO")]
    public string CustomerNo { get; init; } = default!;

    [JsonPropertyName("CUST_NM")]
    public string CustomerName { get; init; } = default!;

    /// <summary>A: Aktif, P: Pasif, B: Bloke</summary>
    [JsonPropertyName("STAT_CD")]
    public string StatusCode { get; init; } = default!;

    /// <summary>Örn. "50.000,00" (binlik ayırıcı nokta, ondalık ayırıcı virgül)</summary>
    [JsonPropertyName("CRD_LMT")]
    public string CreditLimit { get; init; } = default!;

    [JsonPropertyName("CRD_USED")]
    public string CreditUsed { get; init; } = default!;

    /// <summary>"TL" veya "TRY"</summary>
    [JsonPropertyName("CCY_CD")]
    public string CurrencyCode { get; init; } = default!;
}
