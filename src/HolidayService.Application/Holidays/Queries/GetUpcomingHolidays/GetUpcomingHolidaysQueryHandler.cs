using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Time;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries.GetUpcomingHolidays;

internal sealed class GetUpcomingHolidaysQueryHandler(HolidayRangeLoader loader, IDateTimeProvider clock)
    : IQueryHandler<GetUpcomingHolidaysQuery, HolidayCollectionResponse>
{
    public async Task<Result<HolidayCollectionResponse>> Handle(
        GetUpcomingHolidaysQuery request,
        CancellationToken cancellationToken)
    {
        Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

        if (countryCode.IsFailure)
        {
            return Result.Failure<HolidayCollectionResponse>(countryCode.Error);
        }

        DateOnly today = clock.Today;
        DateOnly until = today.AddDays(request.Days);

        Result<IReadOnlyList<Holiday>> holidays = await loader
            .LoadAsync(countryCode.Value, today, until, cancellationToken)
            .ConfigureAwait(false);

        if (holidays.IsFailure)
        {
            return Result.Failure<HolidayCollectionResponse>(holidays.Error);
        }

        IReadOnlyList<HolidayResponse> upcoming = holidays.Value.ToResponse();

        return Result.Success(new HolidayCollectionResponse(
            countryCode.Value.Value,
            today,
            until,
            upcoming.Count,
            upcoming));
    }
}
