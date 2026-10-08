namespace Contracts.Grpc.Inventory;

/// <summary>
/// Protobuf DecimalValue mesajı ile .NET decimal arasında dönüşüm.
/// units: tam kısım, nanos: 10^-9 hassasiyetinde ondalık kısım.
/// Protobuf decimal tipleri için önerilen yaklaşım: https://developers.google.com/protocol-buffers/docs/reference/google.protobuf#google.protobuf.DecimalValue
/// </summary>
public partial class DecimalValue
{
    private const decimal NanoFactor = 1_000_000_000;

    public DecimalValue(long units, int nanos)
    {
        Units = units;
        Nanos = nanos;
    }

    public static implicit operator decimal(DecimalValue grpcDecimal) =>
        grpcDecimal.Units + grpcDecimal.Nanos / NanoFactor;

    public static implicit operator DecimalValue(decimal value)
    {
        var units = decimal.ToInt64(value);
        var nanos = decimal.ToInt32((value - units) * NanoFactor);
        return new DecimalValue(units, nanos);
    }
}
