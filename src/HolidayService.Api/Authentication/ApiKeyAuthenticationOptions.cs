using Microsoft.AspNetCore.Authentication;

namespace HolidayService.Api.Authentication;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";

    public string HeaderName { get; set; } = "X-API-Key";
}
