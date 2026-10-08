namespace BuildingBlocks.Abstractions.Results;

/// <summary>
/// Result Pattern: Bir işlemin başarılı mı başarısız mı olduğunu, exception fırlatmadan ifade eder.
/// Metot imzası (Result / Result&lt;T&gt;) işlemin başarısız olabileceğini açıkça gösterir.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        // Tutarsız durumları engelle: başarılı sonuç hata taşıyamaz, başarısız sonuç hatasız olamaz.
        if ((isSuccess && error != Error.None) || (!isSuccess && error == Error.None))
        {
            throw new ArgumentException("Invalid error state for result.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    // "return OrderErrors.NotFound(id);" yazımını mümkün kılar.
    public static implicit operator Result(Error error) => Failure(error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    /// <summary>Başarısız bir sonucun değerine erişmek programlama hatasıdır.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    // Not: C# kuralı gereği interface tipleri (örn. IReadOnlyList<T>) için implicit dönüşüm çalışmaz;
    // bu durumlarda Result.Success<T>(...) / Result.Failure<T>(...) açıkça kullanılır.
    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
