using System.Net;

namespace SchulCloud.Web.Services.Models;

/// <summary>
/// The location information of an resolved ip address.
/// </summary>
/// <param name="Request">The ip address that were used request this data.</param>
/// <param name="continent">The continent</param>
/// <param name="Country"></param>
/// <param name="CountryCode"></param>
/// <param name="Region"></param>
/// <param name="RegionCode"></param>
/// <param name="Latitude"></param>
/// <param name="Longitude"></param>
public record IPGeoLookupResult(IPAddress Request, string continent, string continentCode, string Country, string CountryCode, string Region, string RegionCode, decimal Latitude, decimal Longitude);
