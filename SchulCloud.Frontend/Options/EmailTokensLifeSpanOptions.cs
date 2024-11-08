namespace SchulCloud.Frontend.Options;

/// <summary>
/// Options for token life spans.
/// </summary>
/// <remarks>
/// All of these values are set to one hour by default.
/// If set to <c>null</c> the life time won't be displayed.
/// This doesn't affect the time the token is valid.
/// </remarks>
public class EmailTokensLifeSpanOptions
{
    private static readonly TimeSpan _defaultLifeSpan = TimeSpan.FromHours(1);

    /// <summary>
    /// The time a email confirmation link is valid.
    /// </summary>
    public TimeSpan EmailConfirmLink { get; set; } = _defaultLifeSpan;

    /// <summary>
    /// The time a password reset token is valid.
    /// </summary>
    public TimeSpan PasswordResetToken { get; set; } = _defaultLifeSpan;

    /// <summary>
    /// The time a two factor email token is valid.
    /// </summary>
    public TimeSpan TwoFactorEmailToken { get; set; } = _defaultLifeSpan;
}
