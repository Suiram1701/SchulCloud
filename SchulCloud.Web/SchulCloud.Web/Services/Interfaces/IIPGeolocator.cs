using SchulCloud.Web.Services.Models;
using System.Net;

namespace SchulCloud.Web.Services.Interfaces;

/// <summary>
/// An service that provides a geolocation lookup of an ip address.
/// </summary>
public interface IIPGeolocator
{
    /// <summary>
    /// Tries to find the geolocation of the specified ip address.
    /// </summary>
    /// <param name="address">The ip address to search for.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The information about the geolocation. If <c>null</c> the geolocation of the address could not be resolved.</returns>
    public Task<IPGeoLookupResult?> GetLocationAsync(IPAddress address, CancellationToken ct = default);
}
