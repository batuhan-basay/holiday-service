using System.Collections.Concurrent;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace HolidayService.Application.Messaging;

internal sealed class QueryDispatcher(IServiceProvider services) : IQueryDispatcher
{
    private static readonly ConcurrentDictionary<Type, object> Executors = new();

    public Task<Result<TResponse>> SendAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var executor = (QueryExecutor<TResponse>)Executors.GetOrAdd(
            query.GetType(),
            static queryType => Activator.CreateInstance(
                typeof(QueryExecutor<,>).MakeGenericType(queryType, typeof(TResponse)))!);

        return executor.ExecuteAsync(query, services, cancellationToken);
    }
}

internal abstract class QueryExecutor<TResponse>
{
    public abstract Task<Result<TResponse>> ExecuteAsync(
        IQuery<TResponse> query,
        IServiceProvider services,
        CancellationToken cancellationToken);
}

internal sealed class QueryExecutor<TQuery, TResponse> : QueryExecutor<TResponse>
    where TQuery : IQuery<TResponse>
{
    public override Task<Result<TResponse>> ExecuteAsync(
        IQuery<TResponse> query,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var typed = (TQuery)query;
        var handler = services.GetRequiredService<IQueryHandler<TQuery, TResponse>>();

        QueryHandlerDelegate<TResponse> pipeline = () => handler.Handle(typed, cancellationToken);

        foreach (IQueryBehavior<TQuery, TResponse> behavior in
            services.GetServices<IQueryBehavior<TQuery, TResponse>>().Reverse())
        {
            QueryHandlerDelegate<TResponse> next = pipeline;
            IQueryBehavior<TQuery, TResponse> current = behavior;

            pipeline = () => current.HandleAsync(typed, next, cancellationToken);
        }

        return pipeline();
    }
}
