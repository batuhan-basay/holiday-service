using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Messaging;
using HolidayService.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace HolidayService.UnitTests.Application;

public sealed class QueryDispatcherTests
{
    private sealed record Ping(string Value) : IQuery<string>;

    private sealed record Unregistered : IQuery<string>;

    private sealed class PingHandler : IQueryHandler<Ping, string>
    {
        public Task<Result<string>> Handle(Ping request, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(request.Value));
    }

    private sealed class BehaviorLog
    {
        public List<string> Entries { get; } = [];
    }

    private sealed class OuterBehavior<TQuery, TResponse>(BehaviorLog log)
        : IQueryBehavior<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> HandleAsync(
            TQuery query,
            QueryHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            log.Entries.Add("outer:before");
            Result<TResponse> result = await next();
            log.Entries.Add("outer:after");

            return result;
        }
    }

    private sealed class InnerBehavior<TQuery, TResponse>(BehaviorLog log)
        : IQueryBehavior<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> HandleAsync(
            TQuery query,
            QueryHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            log.Entries.Add("inner:before");
            Result<TResponse> result = await next();
            log.Entries.Add("inner:after");

            return result;
        }
    }

    private static ServiceProvider BuildProvider(BehaviorLog log, bool withBehaviors)
    {
        var services = new ServiceCollection();

        services.AddSingleton(log);
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        services.AddScoped<IQueryHandler<Ping, string>, PingHandler>();

        if (withBehaviors)
        {
            services.AddScoped(typeof(IQueryBehavior<,>), typeof(OuterBehavior<,>));
            services.AddScoped(typeof(IQueryBehavior<,>), typeof(InnerBehavior<,>));
        }

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendAsync_ReturnsTheHandlerResult()
    {
        using ServiceProvider provider = BuildProvider(new BehaviorLog(), withBehaviors: false);
        IQueryDispatcher dispatcher = provider.GetRequiredService<IQueryDispatcher>();

        Result<string> result = await dispatcher.SendAsync(new Ping("pong"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("pong");
    }

    [Fact]
    public async Task SendAsync_WrapsTheHandlerInRegistrationOrder()
    {
        var log = new BehaviorLog();
        using ServiceProvider provider = BuildProvider(log, withBehaviors: true);

        await provider
            .GetRequiredService<IQueryDispatcher>()
            .SendAsync(new Ping("pong"), CancellationToken.None);

        log.Entries.ShouldBe(new[]
        {
            "outer:before",
            "inner:before",
            "inner:after",
            "outer:after"
        });
    }

    [Fact]
    public async Task SendAsync_WorksWithoutAnyBehaviors()
    {
        var log = new BehaviorLog();
        using ServiceProvider provider = BuildProvider(log, withBehaviors: false);

        Result<string> result = await provider
            .GetRequiredService<IQueryDispatcher>()
            .SendAsync(new Ping("pong"), CancellationToken.None);

        result.Value.ShouldBe("pong");
        log.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task SendAsync_FailsLoudlyWhenNoHandlerIsRegistered()
    {
        using ServiceProvider provider = BuildProvider(new BehaviorLog(), withBehaviors: false);
        IQueryDispatcher dispatcher = provider.GetRequiredService<IQueryDispatcher>();

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await dispatcher.SendAsync(new Unregistered(), CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_RejectsANullQuery()
    {
        using ServiceProvider provider = BuildProvider(new BehaviorLog(), withBehaviors: false);
        IQueryDispatcher dispatcher = provider.GetRequiredService<IQueryDispatcher>();

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await dispatcher.SendAsync<string>(null!, CancellationToken.None));
    }
}
