namespace SchulCloud.Web.Options;

/// <summary>
/// Options for a users password reset.
/// </summary>
public class PasswordResetOptions
{
    /// <summary>
    /// The timeout for a user of sending password reset requests.
    /// </summary>
    public TimeSpan ResetTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The displayed time how long the reset link will be stay valid.
    /// </summary>
    /// <remarks>
    /// This doesn't affect the time the token will be valid that has to get configured in the token provider options. If <c>null</c> the time won't be displayed.
    /// </remarks>
    public TimeSpan? DisplayedTokenLifespan { get; set; }
}
