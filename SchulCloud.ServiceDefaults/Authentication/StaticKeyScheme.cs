using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace SchulCloud.ServiceDefaults.Authentication;

internal class StaticKeyScheme(
    IOptionsMonitor<StaticKeySchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<StaticKeySchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(Options.Key))
        {
            return Task.FromResult(AuthenticateResult.Success(GetAuthenticationTicket()));
        }

        if (Context.Request.Headers["x-api-key"] == Options.Key)
        {
            return Task.FromResult(AuthenticateResult.Success(GetAuthenticationTicket()));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    public AuthenticationTicket GetAuthenticationTicket()
    {
        ClaimsIdentity identity = new(authenticationType: "static-key");
        return new(new(identity), Scheme.Name);
    }
}
