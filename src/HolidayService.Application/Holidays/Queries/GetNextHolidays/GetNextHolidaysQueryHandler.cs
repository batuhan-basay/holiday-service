using HolidayService.Application.Abstractions;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Time;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries.GetNextHolidays;

internal sealed class GetNextHolidaysQueryHandler(HolidayRangeLoader loader, IDateTimeProvider clock)
    : IQueryHandler<GetNextHolidaysQuery, HolidayCollectionResponse>
{
    private const int LookAheadYears = 2;

    public async Task<Result<HolidayCollectionResponse>> Handle(
        GetNextHolidaysQuery request,
        CancellationToken cancellationToken)
    {
        Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

        if (countryCode.IsFailure)
        {
            return Result.Failure<HolidayCollectionResponse>(countryCode.Error);
        }

        DateOnly today = clock.Today;
        DateOnly horizon = today.AddYears(LookAheadYears);

        if (!SupportedYears.Contains(horizon.Year))
        {
            horizon = new DateOnly(SupportedYears.Max, 12, 31);
        }

        Result<IReadOnlyList<Holiday>> holidays = await loader
            .LoadAsync(countryCode.Value, today, horizon, cancellationToken)
            .ConfigureAwait(false);

        if (holidays.IsFailure)
        {
            return Result.Failure<HolidayCollectionResponse>(holidays.Error);
        }

        IReadOnlyList<HolidayResponse> next = holidays.Value
            .Where(holiday => holiday.Date >= today)
            .OrderBy(static holiday => holiday.Date)
            .Take(request.Count)
            .ToResponse();

        DateOnly to = next.Count > 0 ? next[^1].Date : today;

        return Result.Success(new HolidayCollectionResponse(
            countryCode.Value.Value,
            today,
            to,
            next.Count,
            next));
    }
}
