using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;
using Microsoft.Extensions.Options;

namespace HolidayService.Infrastructure.Calendars;

internal sealed class EmbeddedCalendarCatalog
{
    private const string ResourcePrefix = "HolidayService.Infrastructure.Calendars.Data.";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly FrozenDictionary<string, HolidayCalendar> _calendars;

    public EmbeddedCalendarCatalog(IOptions<HolidayDataOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        int adjustment = options.Value.HijriDayAdjustment;
        Assembly assembly = typeof(EmbeddedCalendarCatalog).Assembly;

        Dictionary<string, HolidayCalendar> loaded = new(StringComparer.Ordinal);

        foreach (string resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                || !resource.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }

            using Stream? stream = assembly.GetManifestResourceStream(resource);

            if (stream is null)
            {
                continue;
            }

            CalendarDefinition definition =
                JsonSerializer.Deserialize<CalendarDefinition>(stream, SerializerOptions)
                ?? throw new InvalidOperationException($"'{resource}' is not a calendar definition.");

            HolidayCalendar calendar = definition.ToDomain(adjustment);
            loaded[calendar.Code.Value] = calendar;
        }

        if (loaded.Count == 0)
        {
            throw new InvalidOperationException(
                "No holiday calendars were embedded in the infrastructure assembly.");
        }

        _calendars = loaded.ToFrozenDictionary(StringComparer.Ordinal);

        Countries =
        [
            .. _calendars.Values
                .Select(static calendar => calendar.ToSummary())
                .OrderBy(static country => country.Name, StringComparer.Ordinal)
        ];
    }

    public IReadOnlyList<CountrySummary> Countries { get; }

    public bool TryGet(CountryCode code, out HolidayCalendar calendar)
    {
        if (_calendars.TryGetValue(code.Value, out HolidayCalendar? found))
        {
            calendar = found;
            return true;
        }

        calendar = null!;
        return false;
    }
}
