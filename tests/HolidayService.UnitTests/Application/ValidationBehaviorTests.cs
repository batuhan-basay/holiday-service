using FluentValidation;
using HolidayService.Application.Behaviors;
using HolidayService.Application.Exceptions;
using HolidayService.Application.Holidays.Queries;
using HolidayService.Application.Contracts;
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

    [Fact]
    public async Task Handle_InvokesTheHandlerWhenThereAreNoValidators()
    {
        var behavior = new ValidationBehavior<GetHolidaysByYear.Query, Result<HolidayCollectionResponse>>([]);
        bool handlerCalled = false;

        Result<HolidayCollectionResponse> result = await behavior.Handle(
            new GetHolidaysByYear.Query("TR", 2026),
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
    public async Task Handle_ThrowsAndSkipsTheHandlerWhenValidationFails()
    {
        IValidator<GetHolidaysByYear.Query>[] validators = [new GetHolidaysByYear.Validator()];
        var behavior = new ValidationBehavior<GetHolidaysByYear.Query, Result<HolidayCollectionResponse>>(validators);
        bool handlerCalled = false;

        ValidationAppException exception = await Should.ThrowAsync<ValidationAppException>(
            async () => await behavior.Handle(
                new GetHolidaysByYear.Query("TURKEY", 1800),
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
    public async Task Handle_PassesValidRequestsThrough()
    {
        IValidator<GetHolidaysByYear.Query>[] validators = [new GetHolidaysByYear.Validator()];
        var behavior = new ValidationBehavior<GetHolidaysByYear.Query, Result<HolidayCollectionResponse>>(validators);

        Result<HolidayCollectionResponse> result = await behavior.Handle(
            new GetHolidaysByYear.Query("TR", 2026),
            () => Task.FromResult(HandlerResult),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }
}
