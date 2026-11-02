using FluentValidation;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Behaviors;
using HolidayService.Application.Contracts;
using HolidayService.Application.Exceptions;
using HolidayService.Application.Holidays.Queries.GetHolidaysByYear;
using HolidayService.Domain.Common;

namespace HolidayService.UnitTests.Application;

public sealed class ValidationBehaviorTests
{
    private static readonly Result<HolidayCollectionResponse> HandlerResult =
        Result.Success(new HolidayCollectionResponse(
            "TR",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            0,
            []));

    private static ValidationBehavior<GetHolidaysByYearQuery, HolidayCollectionResponse> Behavior(
        params IValidator<GetHolidaysByYearQuery>[] validators) => new(validators);

    [Fact]
    public async Task HandleAsync_InvokesTheHandlerWhenThereAreNoValidators()
    {
        bool handlerCalled = false;

        Result<HolidayCollectionResponse> result = await Behavior().HandleAsync(
            new GetHolidaysByYearQuery("TR", 2026),
            () =>
            {
                handlerCalled = true;
                return Task.FromResult(HandlerResult);
            },
            CancellationToken.None);

        handlerCalled.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ThrowsAndSkipsTheHandlerWhenValidationFails()
    {
        bool handlerCalled = false;

        ValidationAppException exception = await Should.ThrowAsync<ValidationAppException>(
            async () => await Behavior(new GetHolidaysByYearQueryValidator()).HandleAsync(
                new GetHolidaysByYearQuery("TURKEY", 1800),
                () =>
                {
                    handlerCalled = true;
                    return Task.FromResult(HandlerResult);
                },
                CancellationToken.None));

        handlerCalled.ShouldBeFalse();
        exception.Errors.Keys.ShouldContain("CountryCode");
        exception.Errors.Keys.ShouldContain("Year");
    }

    [Fact]
    public async Task HandleAsync_PassesValidRequestsThrough()
    {
        Result<HolidayCollectionResponse> result =
            await Behavior(new GetHolidaysByYearQueryValidator()).HandleAsync(
                new GetHolidaysByYearQuery("TR", 2026),
                () => Task.FromResult(HandlerResult),
                CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ReportsEveryFailingPropertyAtOnce()
    {
        ValidationAppException exception = await Should.ThrowAsync<ValidationAppException>(
            async () => await Behavior(new GetHolidaysByYearQueryValidator()).HandleAsync(
                new GetHolidaysByYearQuery(string.Empty, 99999),
                () => Task.FromResult(HandlerResult),
                CancellationToken.None));

        exception.Errors.Count.ShouldBe(2);
    }
}
