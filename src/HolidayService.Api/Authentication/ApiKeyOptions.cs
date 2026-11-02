namespace HolidayService.Api.Authentication;

/// <summary>
/// Configured API keys. Keys are never committed: development uses the value in
/// appsettings.Development.json, other environments supply them through
/// <c>ApiKeys__Clients__0__Key</c> style environment variables or user-secrets.
/// </summary>
public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKeys";

    /// <summary>Set to false to run the API open, e.g. for a public demo deployment.</summary>
    public bool Enabled { get; set; } = true;

    public IList<ApiKeyClient> Clients { get; init; } = [];
}

public sealed class ApiKeyClient
{
    /// <summary>Human-readable owner of the key; surfaced as the authenticated identity.</summary>
    public string Owner { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;
}
