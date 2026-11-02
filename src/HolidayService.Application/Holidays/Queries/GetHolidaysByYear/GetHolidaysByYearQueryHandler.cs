using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries.GetHolidaysByYear;

internal sealed class GetHolidaysByYearQueryHandler(IHolidayProvider provider)
    : IQueryHandler<GetHolidaysByYearQuery, HolidayCollectionResponse>
{
    public async Task<Result<HolidayCollectionResponse>> Handle(
        GetHolidaysByYearQuery request,
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

        IReadOnlyList<HolidayResponse> filtered = holidays.Value
            .Apply(request.Type)
            .OrderBy(static holiday => holiday.Date)
            .ToResponse();

        return Result.Success(new HolidayCollectionResponse(
            countryCode.Value.Value,
            new DateOnly(request.Year, 1, 1),
            new DateOnly(request.Year, 12, 31),
            filtered.Count,
            filtered));
    }
}
