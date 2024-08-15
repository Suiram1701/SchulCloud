using Humanizer;
using Microsoft.Extensions.Options;
using SchulCloud.Database.Models;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Options;
using SchulCloud.Web.Services.Interfaces;
using System.Globalization;

namespace SchulCloud.Web.Identity.EmailSenders;

/// <summary>
/// Provides a email sender that generates a localized UI for the sendet emails
/// </summary>
public abstract class EmailSenderBase(ILogger logger, IServiceProvider serviceProvider) : IEmailSender<User>
{
    protected readonly ILogger _logger = logger;
    private readonly PasswordResetOptions _passwordResetOptions = serviceProvider.GetRequiredService<IOptions<PasswordResetOptions>>().Value;

    // A real UI and localization will be implemented later.
    public virtual async Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        string content = string.Format(
            "A reset of the account password of this account was requested. " +
            "Go on {0} to reset the password. This link will expire in {1}. " +
            "If you didn't requested this ignore this Email.",
            resetLink, _passwordResetOptions.DisplayedTokenLifespan.Humanize(culture: CultureInfo.InvariantCulture));
        await ExecuteInternalAsync(user, email, "Reset account password requested", content);
    }

    private async Task ExecuteInternalAsync(User user, string email, string subject, string content)
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
    protected abstract Task ExecuteAsync(User user, string email, string subject, string content);
}
