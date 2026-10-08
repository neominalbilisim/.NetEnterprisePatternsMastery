using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Abstractions.Results;
using FluentValidation;
using Ordering.Application.Ports;

namespace Ordering.Application.Orders.Queries;

public sealed record GetCustomerOrdersQuery(string CustomerCode, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<OrderSummaryResponse>>;

internal sealed class GetCustomerOrdersQueryValidator : AbstractValidator<GetCustomerOrdersQuery>
{
    public GetCustomerOrdersQueryValidator()
    {
        RuleFor(x => x.CustomerCode).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

internal sealed class GetCustomerOrdersQueryHandler(IOrderReadRepository readRepository)
    : IQueryHandler<GetCustomerOrdersQuery, PagedResult<OrderSummaryResponse>>
{
    public async Task<Result<PagedResult<OrderSummaryResponse>>> Handle(
        GetCustomerOrdersQuery query, CancellationToken cancellationToken)
    {
        var result = await readRepository.GetByCustomerAsync(query.CustomerCode, query.Page, query.PageSize, cancellationToken);
        return result;
    }
}
