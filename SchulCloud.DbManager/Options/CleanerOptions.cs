namespace SchulCloud.DbManager.Options;

public class CleanerOptions
{
    /// <summary>
    /// The time interval between cleaner executions.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// A timeout for each cleanup cycle.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The maximum amount of login attempts per user before removing the oldest ones.
    /// </summary>
    /// <remarks>
    /// -1 means that there is not limit.
    /// </remarks>
    public int MaxLoginAttemptsPerUser { get; set; } = -1;
}
