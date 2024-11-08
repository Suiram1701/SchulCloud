using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SchulCloud.Frontend.Options;
using System.Globalization;

namespace SchulCloud.Frontend.Identity.EmailSenders;

/// <summary>
/// Provides a email sender that generates a localized UI for the sendet emails
/// </summary>
public abstract class EmailSenderBase<TUser>(ILogger logger, IOptions<EmailSenderOptions> optionsAccessor, UserManager<TUser> userManager) : IEmailSender<TUser>
    where TUser : class
{
    protected readonly ILogger _logger = logger;
    protected readonly UserManager<TUser> _userManager = userManager;
    private readonly EmailSenderOptions _options = optionsAccessor.Value;

    // A real UI and localization will be implemented later.
    public virtual async Task SendEmailConfirmLinkAsync(TUser user, string email, string confirmLink)
    {
        string userName = (await _userManager.GetUserNameAsync(user))!;

        string content = string.Format(
            "A confirmation of this email for the user account {0} was requested. " +
            "Go on {1} to confirm this email address for the account. This link will expire in {2}. " +
            "If you didn't created this account ignore this email.",
            userName, confirmLink, _options.TokensLifeSpans.EmailConfirmLink.Humanize(culture: CultureInfo.InvariantCulture));
        await ExecuteInternalAsync(user, email, "Email confirmation", content);
    }

    public virtual async Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink)
    {
        string content = string.Format(
            "A reset of the account password of this account was requested. " +
            "Go on {0} to reset the password. This link will expire in {1}. " +
            "If you didn't requested this ignore this Email.",
            resetLink, _options.TokensLifeSpans.PasswordResetToken.Humanize(culture: CultureInfo.InvariantCulture));
        await ExecuteInternalAsync(user, email, "Reset account password", content);
    }

    public virtual async Task Send2faEmailCodeAsync(TUser user, string email, string code)
    {
        string content = string.Format(
            "A 2fa authentication via email was requested for this account. " +
            "The authentication code is {0}. This code will expire in {1}. " +
            "If you didn't requested this your password may compromised and you should change it.",
            code, _options.TokensLifeSpans.TwoFactorEmailToken.Humanize(culture: CultureInfo.InvariantCulture));
        await ExecuteInternalAsync(user, email, "2FA Email confirmation", content);
    }

    private async Task ExecuteInternalAsync(TUser user, string email, string subject, string content)
    {
        try
        {
            _logger.LogInformation("Send Email to {email}", email);
            await ExecuteAsync(user, email, subject, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending an email to {email}.", email);
            throw;
        }
    }

    /// <summary>
    /// Sends an email.
    /// </summary>
    /// <param name="user">The user that issued this email.</param>
    /// <param name="email">The email to sent this mail to.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="content">The raw content of the email</param>
    /// <returns>A task that returns the result of the email sending.</returns>
    protected abstract Task ExecuteAsync(TUser user, string email, string subject, string content);
}
