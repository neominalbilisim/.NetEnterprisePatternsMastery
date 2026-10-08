using BuildingBlocks.Abstractions.Results;
using MediatR;

namespace BuildingBlocks.Abstractions.Messaging;

// CQRS sözleşmeleri: Command (yazma) ve Query (okuma) ayrı arayüzlerle ifade edilir.
// Tüm istekler Result / Result<T> döndürür; böylece pipeline behavior'lar hata akışını tip güvenli yönetebilir.

/// <summary>Tüm command'ların ortak işaretleyicisi (TransactionBehavior bunu hedefler).</summary>
public interface IBaseCommand
{
}

/// <summary>Değer döndürmeyen command.</summary>
public interface ICommand : IRequest<Result>, IBaseCommand
{
}

/// <summary>Değer döndüren command (örn. oluşturulan kaydın Id'si).</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand
{
}

/// <summary>Yan etkisiz okuma isteği.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
