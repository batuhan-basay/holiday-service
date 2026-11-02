using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Calendar;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries.GetLongWeekends;

internal sealed class GetLongWeekendsQueryHandler(IHolidayProvider provider)
    : IQueryHandler<GetLongWeekendsQuery, IReadOnlyList<LongWeekendResponse>>
{
    public async Task<Result<IReadOnlyList<LongWeekendResponse>>> Handle(
        GetLongWeekendsQuery request,
        CancellationToken cancellationToken)
    {
        Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

        if (countryCode.IsFailure)
        {
            return Result.Failure<IReadOnlyList<LongWeekendResponse>>(countryCode.Error);
        }

        Result<IReadOnlyList<Holiday>> holidays = await provider
            .GetHolidaysAsync(countryCode.Value, request.Year, cancellationToken)
            .ConfigureAwait(false);

        if (holidays.IsFailure)
        {
            return Result.Failure<IReadOnlyList<LongWeekendResponse>>(holidays.Error);
        }

        var calendar = new WorkCalendar(countryCode.Value, holidays.Value);

        IReadOnlyList<LongWeekendResponse> response =
        [
            .. calendar
                .FindLongWeekends(request.Year, request.IncludeBridgeDays)
                .Select(static weekend => weekend.ToResponse())
        ];

        return Result.Success(response);
    }
}
