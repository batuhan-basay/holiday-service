using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries.GetHolidaysByRange;

internal sealed class GetHolidaysByRangeQueryHandler(HolidayRangeLoader loader)
    : IQueryHandler<GetHolidaysByRangeQuery, HolidayCollectionResponse>
{
    public async Task<Result<HolidayCollectionResponse>> Handle(
        GetHolidaysByRangeQuery request,
        CancellationToken cancellationToken)
    {
        Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

        if (countryCode.IsFailure)
        {
            return Result.Failure<HolidayCollectionResponse>(countryCode.Error);
        }

        int days = request.To.DayNumber - request.From.DayNumber + 1;

        if (days > GetHolidaysByRangeQuery.MaxRangeDays)
        {
            return Result.Failure<HolidayCollectionResponse>(
                HolidayErrors.RangeTooWide(days, GetHolidaysByRangeQuery.MaxRangeDays));
        }

        Result<IReadOnlyList<Holiday>> holidays = await loader
            .LoadAsync(countryCode.Value, request.From, request.To, cancellationToken)
            .ConfigureAwait(false);

        if (holidays.IsFailure)
        {
            return Result.Failure<HolidayCollectionResponse>(holidays.Error);
        }

        IReadOnlyList<HolidayResponse> response = holidays.Value.ToResponse();

        return Result.Success(new HolidayCollectionResponse(
            countryCode.Value.Value,
            request.From,
            request.To,
            response.Count,
            response));
    }
}
