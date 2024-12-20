using Microsoft.AspNetCore.Authentication;

namespace SchulCloud.ServiceDefaults.Authentication;

internal class StaticKeySchemeOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// The static key to use.
    /// </summary>
    public string? Key { get; set; }
}
