using Newtonsoft.Json;

namespace SchulCloud.Frontend.Services.Models;

/// <summary>
/// The settings for cookies.
/// </summary>
public class CookieConsentSettings
{
    /// <summary>
    /// Indicates whether necessary cookies are allowed (always true).
    /// </summary>
    [JsonProperty("strictly-necessary")]
    public bool Necessary { get; set; }

    /// <summary>
    /// Indicates whether functional cookies are allowed.
    /// </summary>
    [JsonProperty("functionality")]
    public bool Functionality { get; set; }

    /// <summary>
    /// Indicates whether tracking cookies are allowed.
    /// </summary>
    [JsonProperty("tracking")]
    public bool Tracking { get; set; }

    /// <summary>
    /// Indicates whether tracking cookies are allowed.
    /// </summary>
    [JsonProperty("targeting")]
    public bool Targeting { get; set; }
}
