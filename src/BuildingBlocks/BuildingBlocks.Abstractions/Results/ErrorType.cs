namespace BuildingBlocks.Abstractions.Results;

/// <summary>
/// Hata türü. API katmanında HTTP durum koduna, gRPC katmanında StatusCode'a çevrilir.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unavailable = 4
}
