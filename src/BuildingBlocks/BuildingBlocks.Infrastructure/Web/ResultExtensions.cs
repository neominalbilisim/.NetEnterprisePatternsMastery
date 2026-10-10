using BuildingBlocks.Abstractions.Results;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Infrastructure.Web;

/// <summary>
/// Result -> HTTP yanıtı dönüşümü. Hatalar RFC 7807 Problem Details formatında döner.
/// </summary>
public static class ResultExtensions
{
    public static IResult Match<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.ToProblem();

    public static IResult Match(this Result result, Func<IResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : result.ToProblem();

  // Uygulama içerisinde eğer herhangi bir Result nesnesi başarısız ise, bu metot ile HTTP yanıtı olarak Problem Details formatında dönebiliriz.
  public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be converted to a problem.");
        }

        var error = result.Error;
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status422UnprocessableEntity
        };

        var extensions = new Dictionary<string, object?> { ["errorCode"] = error.Code };
        if (error is ValidationError validationError)
        {
            extensions["errors"] = validationError.Errors;
        }

        return Results.Problem(
            statusCode: statusCode,
            title: GetTitle(error.Type),
            detail: error.Description,
            type: $"https://httpstatuses.io/{statusCode}",
            extensions: extensions);
    }

    private static string GetTitle(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unavailable => "Service unavailable",
        _ => "Business rule violation"
    };
}
