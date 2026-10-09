using FluentValidation;

namespace Ordering.Application.Customers.Commands.CreateCustomer;

internal sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[A-Za-z0-9\-_]+$")
            .WithMessage("Code must contain only letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .MinimumLength(2);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(255)
            .EmailAddress()
            .WithMessage("Email address is invalid.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.TaxId)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.TaxId));
    }
}
