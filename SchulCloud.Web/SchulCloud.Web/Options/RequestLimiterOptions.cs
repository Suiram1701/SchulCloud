namespace SchulCloud.Web.Options;

/// <summary>
/// Options the request timeouts.
/// </summary>
public class RequestLimiterOptions
{
    /// <summary>
    /// The timeout for a user of sending password reset requests.
    /// </summary>
    /// <remarks>
    /// Default value is one minute.
    /// </remarks>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// A mapping of purpose to timeout.
    /// </summary>
    public Dictionary<string, TimeSpan> Timeouts { get; set; } = [];
}
