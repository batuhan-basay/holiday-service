using HolidayService.Domain.Common;

namespace HolidayService.Application.Abstractions.Messaging;

public interface IQueryDispatcher
{
    Task<Result<TResponse>> SendAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default);
}
