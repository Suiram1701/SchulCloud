using MailKit.Client;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;
using SchulCloud.Database.Models;
using SchulCloud.Web.Options;
using System.Net.Mail;

namespace SchulCloud.Web.Identity.EmailSenders;

public class MailKitEmailSender(ILogger<MailKitEmailSender> logger, IOptionsSnapshot<EmailSenderOptions> optionsSnapshot, MailKitClientFactory clientFactory) : EmailSenderBase(logger)
{
    private readonly IOptionsSnapshot<EmailSenderOptions> _optionsSnapshot = optionsSnapshot;
    private readonly MailKitClientFactory _clientFactory = clientFactory;

    protected override async Task<IdentityResult> ExecuteAsync(User user, string email, string subject, string content)
    {
        EmailSenderOptions options = _optionsSnapshot.Value;

        using MailMessage mailMessage = new()
        {
            From = new(options.Email, options.DisplayedName),
            Subject = subject,
            Body = content
        };
        mailMessage.To.Add(new MailAddress(email));

        ISmtpClient smtpClient = await _clientFactory.GetSmtpClientAsync().ConfigureAwait(false);
        await smtpClient.SendAsync(MimeMessage.CreateFromMailMessage(mailMessage)).ConfigureAwait(false);

        return IdentityResult.Success;
    }
}
