using System.Text.Json.Serialization;

// Legacy ERP simülasyonu: eski tip isimlendirme, metin olarak tutulan Türkçe formatlı tutarlar,
// tek harfli durum kodları. Ayrıca "chaos" ayarlarıyla hata / gecikme üretir.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new ChaosSettings
{
    FailureRate = builder.Configuration.GetValue<double>("Chaos:FailureRate"),
    DelayMs = builder.Configuration.GetValue<int>("Chaos:DelayMs")
});

var app = builder.Build();

var customers = new Dictionary<string, ErpCustomer>(StringComparer.OrdinalIgnoreCase)
{
    // Aktif müşteri, yeterli kredi
    ["C-1001"] = new("C-1001", "AHMET YILMAZ        ", "A", "50.000,00", "5.000,00", "TL"),
    // Bloke müşteri -> sipariş veremez
    ["C-1002"] = new("C-1002", "AYSE KAYA           ", "B", "30.000,00", "0,00", "TL"),
    // Aktif ama kredisi neredeyse dolu -> limit aşımı senaryosu
    ["C-1003"] = new("C-1003", "MEHMET DEMIR        ", "A", "10.000,00", "9.500,00", "TRY")
};

app.MapGet("/erp/api/v1/musteri/{custNo}", async (
    string custNo, ChaosSettings chaos, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    if (chaos.DelayMs > 0)
    {
        logger.LogWarning("Chaos: delaying response by {DelayMs} ms", chaos.DelayMs);
        await Task.Delay(chaos.DelayMs, cancellationToken);
    }

    if (chaos.FailureRate > 0 && Random.Shared.NextDouble() < chaos.FailureRate)
    {
        logger.LogWarning("Chaos: returning 503 for customer {CustomerNo}", custNo);
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    return customers.TryGetValue(custNo, out var customer)
        ? Results.Json(customer)
        : Results.Json(new ErpError("MUS404", "MUSTERI BULUNAMADI"), statusCode: StatusCodes.Status404NotFound);
});

// Chaos ayarlarını çalışma anında değiştirmek için (Postman "Resilience Demo" klasörü)
app.MapGet("/chaos", (ChaosSettings chaos) => Results.Ok(chaos));
app.MapPost("/chaos", (ChaosSettings chaos, ChaosUpdate update) =>
{
    chaos.FailureRate = Math.Clamp(update.FailureRate, 0, 1);
    chaos.DelayMs = Math.Max(0, update.DelayMs);
    return Results.Ok(chaos);
});

app.Run();

public sealed class ChaosSettings
{
    public double FailureRate { get; set; }
    public int DelayMs { get; set; }
}

public sealed record ChaosUpdate(double FailureRate, int DelayMs);

public sealed record ErpCustomer(
    [property: JsonPropertyName("CUST_NO")] string CustNo,
    [property: JsonPropertyName("CUST_NM")] string CustNm,
    [property: JsonPropertyName("STAT_CD")] string StatCd,
    [property: JsonPropertyName("CRD_LMT")] string CrdLmt,
    [property: JsonPropertyName("CRD_USED")] string CrdUsed,
    [property: JsonPropertyName("CCY_CD")] string CcyCd);

public sealed record ErpError(
    [property: JsonPropertyName("HATA_KD")] string ErrorCode,
    [property: JsonPropertyName("HATA_ACK")] string ErrorMessage);

