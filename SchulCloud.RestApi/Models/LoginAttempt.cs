using Mapster;
using SchulCloud.Identity.Enums;
using System.Net;

namespace SchulCloud.RestApi.Models;

/// <summary>
/// A login attempt on the web interface.
/// </summary>
/// <param name="Id">The internal id of this attempt.</param>
/// <param name="Method">The method used for the attempt.</param>
/// <param name="Result">The result of the attempt.</param>
/// <param name="IpAddress">The ip address of the client tried to login.</param>
/// <param name="Latitude">The approximate latitude of the client if locate on login.</param>
/// <param name="Longitude">The approximate longitude of the client if locate on login.</param>
/// <param name="UserAgent">The row user agent the client used to request the login.</param>
/// <param name="DateTime">The UTC date time the login occurred.</param>
public record LoginAttempt(
    string Id,
    LoginAttemptMethod Method,
    LoginAttemptResult Result,
    string IpAddress,
    decimal? Latitude,
    decimal? Longitude,
    string? UserAgent,
    DateTime DateTime)
{
    internal static readonly TypeAdapterConfig _adapterConfig = new TypeAdapterConfig()
        .ForDestinationType<LoginAttempt>().Config
        .ForType<IPAddress, string>().MapWith(address => address.ToString())
        .Config;
}