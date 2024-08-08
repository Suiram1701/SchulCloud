using Microsoft.AspNetCore.Identity;
using SchulCloud.Database.Models;
using System.Text;

namespace SchulCloud.Web.Identity.EmailSenders;

/// <summary>
/// Provides a email sender that generates a localized UI for the sendet emails
/// </summary>
public abstract class EmailSenderBase(ILogger logger) : IEmailSender<User>
{
    protected ILogger _logger = logger;

    // A real UI and localization will be implemented later.
    public virtual async Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        await ExecuteInternalAsync(user, email, "Reset account password requested", $"A reset of the account password of this account was requested. Go on {resetLink} to reset the password. If you didn't requested this ignore this Email.");
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
