namespace SchulCloud.Web.Options;

/// <summary>
/// Options for the password reset.
/// </summary>
public class PasswordResetOptions
{
    /// <summary>
    /// The displayed time the sent token is valid.
    /// </summary>
    /// <remarks>
    /// This is by default one hour.
    /// This doesn't affect the time the token is valid.
    /// </remarks>
    public TimeSpan DisplayedTokenLifespan { get; set; } = TimeSpan.FromHours(1);
}
