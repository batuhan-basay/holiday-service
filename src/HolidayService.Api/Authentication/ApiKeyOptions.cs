namespace HolidayService.Api.Authentication;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKeys";

    public bool Enabled { get; set; } = true;

    public IList<ApiKeyClient> Clients { get; init; } = [];
}

public sealed class ApiKeyClient
{
    public string Owner { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;
}
