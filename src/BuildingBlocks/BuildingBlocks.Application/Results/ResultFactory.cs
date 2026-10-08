using System.Reflection;
using System.Text.Json;
using BuildingBlocks.Abstractions.Results;

namespace BuildingBlocks.Application.Results;

/// <summary>
/// Pipeline behavior'lar generic TResponse (Result veya Result&lt;T&gt;) ile çalışır.
/// Bu yardımcı, TResponse tipine uygun başarılı/başarısız sonucu reflection ile üretir.
/// </summary>
internal static class ResultFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly MethodInfo GenericFailureMethod = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Result.Failure) && m.IsGenericMethodDefinition);

    private static readonly MethodInfo GenericSuccessMethod = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Result.Success) && m.IsGenericMethodDefinition);

    public static TResponse Failure<TResponse>(Error error)
        where TResponse : Result
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        return (TResponse)GenericFailureMethod.MakeGenericMethod(valueType).Invoke(null, [error])!;
    }

    /// <summary>Idempotency kaydından (JSON) başarılı sonucu yeniden oluşturur.</summary>
    public static TResponse SuccessFromJson<TResponse>(string? valueJson)
        where TResponse : Result
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Success();
        }

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var value = valueJson is null ? null : JsonSerializer.Deserialize(valueJson, valueType, JsonOptions);
        return (TResponse)GenericSuccessMethod.MakeGenericMethod(valueType).Invoke(null, [value])!;
    }

    /// <summary>Result&lt;T&gt; değerini JSON olarak saklamak için serialize eder.</summary>
    public static string? SerializeValue(Result result)
    {
        var type = result.GetType();
        if (!type.IsGenericType)
        {
            return null;
        }

        var value = type.GetProperty(nameof(Result<object>.Value))!.GetValue(result);
        return JsonSerializer.Serialize(value, JsonOptions);
    }
}
