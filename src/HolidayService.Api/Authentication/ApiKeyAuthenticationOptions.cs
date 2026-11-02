using Microsoft.AspNetCore.Authentication;

namespace HolidayService.Api.Authentication;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";

    /// <summary>Header the key is read from.</summary>
    public string HeaderName { get; set; } = "X-API-Key";
}
