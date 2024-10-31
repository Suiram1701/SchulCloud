using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using SchulCloud.Web.Services.Interfaces;
using SchulCloud.Web.Services.Models;
using System.Globalization;
using System.Net;

namespace SchulCloud.Web.Services;

/// <summary>
/// An implementation of <see cref="IIPGeolocator"/> that uses the free api of ip-api.com to do the geo lookup.
/// </summary>
public class IPApiGeolocator(ILogger<IPApiGeolocator> logger, HttpClient client) : IIPGeolocator
{
    private readonly CultureInfo[] _supportedCultures = [
        new("en"),
        new("de"),
        new("es"),
        new("pt-BR"),
        new("fr"),
        new("ja"),
        new("zh-CN"),
        new("ru"),
        ];

    public async Task<IPGeoLookupResult?> GetLocationAsync(IPAddress address, CancellationToken ct = default) =>
        await GetLocationAsync(address, CultureInfo.CurrentUICulture, ct);

    /// <inheritdoc cref="GetLocationAsync(IPAddress, CancellationToken)" />
    /// <param name="culture">The culture to use to localize most of the data.</param>
    public async Task<IPGeoLookupResult?> GetLocationAsync(IPAddress address, CultureInfo culture, CancellationToken ct = default)
    {
        CultureInfo? requestCulture = _supportedCultures.Contains(culture)
            ? culture
            : _supportedCultures.FirstOrDefault(supportedCulture => supportedCulture.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName);
        requestCulture ??= _supportedCultures[0];

        try
        {
            Uri requestUri = new UriBuilder
            {
                Scheme = Uri.UriSchemeHttp,     // https isn't permitted in free api access.
                Host = "ip-api.com",
                Path = $"/json/{address}",
                Query = $"fields=3170511&lang={culture}"     // 3170511 represents every field that is needed here.
            }.Uri;
            string responseStr = await client.GetStringAsync(requestUri, ct);

            JObject responseObj = JObject.Parse(responseStr);
            if (responseObj["status"]!.Value<string>() == "success")
            {
                return new(
                    IPAddress.Parse(responseObj["query"]!.Value<string>()!),
                    responseObj["continent"]!.Value<string>()!,
                    responseObj["continentCode"]!.Value<string>()!,
                    responseObj["country"]!.Value<string>()!,
                    responseObj["countryCode"]!.Value<string>()!,
                    responseObj["regionName"]!.Value<string>()!,
                    responseObj["region"]!.Value<string>()!,
                    responseObj["lat"]!.Value<decimal>()!,
                    responseObj["lon"]!.Value<decimal>()!
                    );
            }
            else
            {
                string errorMessage = responseObj["message"]!.Value<string>()!;
                logger.LogDebug("Could not lookup ip address {ipAddress}. Error message: {errorMessage}", address, errorMessage);

                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception occurred while lookup ip address {ipAddress}", address);
            return null;
        }
    }
}
