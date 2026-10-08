namespace BuildingBlocks.Abstractions.Results;

/// <summary>
/// Beklenen (iş kuralı kaynaklı) bir hatayı temsil eder. Exception yerine Result içinde taşınır.
/// </summary>
public record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Unavailable(string code, string description) => new(code, description, ErrorType.Unavailable);
}

/// <summary>
/// Birden fazla doğrulama hatasını tek bir Error olarak taşır (ValidationBehavior tarafından üretilir).
/// </summary>
public sealed record ValidationError(IReadOnlyCollection<ValidationFailureItem> Errors)
    : Error("General.Validation", "One or more validation errors occurred.", ErrorType.Validation);

public sealed record ValidationFailureItem(string PropertyName, string ErrorMessage);
