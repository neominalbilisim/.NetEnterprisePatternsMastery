using BuildingBlocks.Abstractions.Results;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// FluentValidation kurallarını merkezi olarak çalıştırır. Handler'a yalnızca geçerli istekler ulaşır.
/// Doğrulama hatası exception olarak değil, ValidationError içeren bir Result olarak döner.
/// Not: Bu behavior girdi doğrulaması (format, zorunluluk) içindir; iş kuralları domain/handler'da kalır.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => new ValidationFailureItem(f.PropertyName, f.ErrorMessage))
            .Distinct()
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        return ResultFactory.Failure<TResponse>(new ValidationError(failures));
    }
}
