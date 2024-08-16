using MailKit.Client;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;
using SchulCloud.Database.Models;
using SchulCloud.Web.Options;
using System.Net.Mail;

namespace SchulCloud.Web.Identity.EmailSenders;

/// <summary>
/// An <see cref="EmailSenderBase"/> implementation that uses MailKit.Client to send emails.
/// </summary>
public class MailKitEmailSender(ILogger<MailKitEmailSender> logger, IOptions<EmailSenderOptions> optionsAccessor, IServiceProvider serviceProvider, MailKitClientFactory clientFactory)
    : EmailSenderBase(logger, optionsAccessor)
{
    private readonly EmailSenderOptions _options = optionsAccessor.Value;
    private readonly MailKitClientFactory _clientFactory = clientFactory;

    protected override async Task<IdentityResult> ExecuteAsync(User user, string email, string subject, string content)
    {
        using MailMessage mailMessage = new()
        {
            From = new(_options.Email, _options.DisplayedName),
            Subject = subject,
            Body = content
        };
        mailMessage.To.Add(new MailAddress(email));

        ISmtpClient smtpClient = await _clientFactory.GetSmtpClientAsync().ConfigureAwait(false);
        await smtpClient.SendAsync(MimeMessage.CreateFromMailMessage(mailMessage)).ConfigureAwait(false);

        return IdentityResult.Success;
    }
}
