using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HolidayService.Api.Authentication;

internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptionsMonitor<ApiKeyOptions> apiKeys)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ApiKeyOptions configured = apiKeys.CurrentValue;

        if (!configured.Enabled)
        {
            return Task.FromResult(AuthenticateResult.Success(
                CreateTicket("anonymous")));
        }

        if (!Request.Headers.TryGetValue(Options.HeaderName, out Microsoft.Extensions.Primitives.StringValues provided))
        {
            return Task.FromResult(AuthenticateResult.Fail(
                $"The {Options.HeaderName} header is missing."));
        }

        string? presentedKey = provided.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(presentedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail(
                $"The {Options.HeaderName} header is empty."));
        }

        ApiKeyClient? match = configured.Clients
            .FirstOrDefault(client => IsMatch(client.Key, presentedKey));

        if (match is null)
        {
            Logger.LogWarning("Rejected an unrecognised API key");

            return Task.FromResult(AuthenticateResult.Fail("The supplied API key is not valid."));
        }

        return Task.FromResult(AuthenticateResult.Success(CreateTicket(match.Owner)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = $"{Scheme.Name} header=\"{Options.HeaderName}\"";

        return base.HandleChallengeAsync(properties);
    }

    private AuthenticationTicket CreateTicket(string owner)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, owner), new Claim("client_id", owner)],
            Scheme.Name);

        return new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
    }

    private static bool IsMatch(string configuredKey, string presentedKey)
    {
        if (string.IsNullOrEmpty(configuredKey))
        {
            return false;
        }

        byte[] expected = Encoding.UTF8.GetBytes(configuredKey);
        byte[] actual = Encoding.UTF8.GetBytes(presentedKey);

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
