using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries.GetHolidaysByMonth;

internal sealed class GetHolidaysByMonthQueryHandler(IHolidayProvider provider)
    : IQueryHandler<GetHolidaysByMonthQuery, HolidayCollectionResponse>
{
    public async Task<Result<HolidayCollectionResponse>> Handle(
        GetHolidaysByMonthQuery request,
        CancellationToken cancellationToken)
    {
        Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

        if (countryCode.IsFailure)
        {
            return Result.Failure<HolidayCollectionResponse>(countryCode.Error);
        }

        Result<IReadOnlyList<Holiday>> holidays = await provider
            .GetHolidaysAsync(countryCode.Value, request.Year, cancellationToken)
            .ConfigureAwait(false);

        if (holidays.IsFailure)
        {
            return Result.Failure<HolidayCollectionResponse>(holidays.Error);
        }

        var from = new DateOnly(request.Year, request.Month, 1);
        var to = new DateOnly(
            request.Year,
            request.Month,
            DateTime.DaysInMonth(request.Year, request.Month));

        IReadOnlyList<HolidayResponse> inMonth = holidays.Value
            .Where(holiday => holiday.Date >= from && holiday.Date <= to)
            .OrderBy(static holiday => holiday.Date)
            .ToResponse();

        return Result.Success(new HolidayCollectionResponse(
            countryCode.Value.Value,
            from,
            to,
            inMonth.Count,
            inMonth));
    }
}
