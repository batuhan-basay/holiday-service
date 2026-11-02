using HolidayService.Domain.Common;

namespace HolidayService.Application.Abstractions.Messaging;

public delegate Task<Result<TResponse>> QueryHandlerDelegate<TResponse>();

public interface IQueryBehavior<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(
        TQuery query,
        QueryHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
