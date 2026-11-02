using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries.GetWorldwideUpcomingHolidays;

internal sealed class GetWorldwideUpcomingHolidaysQueryHandler(IHolidayProvider provider)
    : IQueryHandler<GetWorldwideUpcomingHolidaysQuery, IReadOnlyList<HolidayResponse>>
{
    public async Task<Result<IReadOnlyList<HolidayResponse>>> Handle(
        GetWorldwideUpcomingHolidaysQuery request,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<Holiday>> holidays = await provider
            .GetNextHolidaysWorldwideAsync(cancellationToken)
            .ConfigureAwait(false);

        return holidays.Map(static list => list
            .OrderBy(static holiday => holiday.Date)
            .ThenBy(static holiday => holiday.CountryCode.Value, StringComparer.Ordinal)
            .ToResponse());
    }
}
