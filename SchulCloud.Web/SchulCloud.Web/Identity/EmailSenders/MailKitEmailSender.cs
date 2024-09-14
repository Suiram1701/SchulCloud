using MailKit.Client;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;
using SchulCloud.Web.Options;
using System.Net.Mail;

namespace SchulCloud.Web.Identity.EmailSenders;

/// <summary>
/// An <see cref="EmailSenderBase"/> implementation that uses MailKit.Client to send emails.
/// </summary>
public class MailKitEmailSender<TUser>(ILogger<MailKitEmailSender<TUser>> logger, IOptions<EmailSenderOptions> optionsAccessor, MailKitClientFactory clientFactory)
    : EmailSenderBase<TUser>(logger, optionsAccessor)
    where TUser : class
{
    private readonly EmailSenderOptions _options = optionsAccessor.Value;
    private readonly MailKitClientFactory _clientFactory = clientFactory;

    protected override async Task<IdentityResult> ExecuteAsync(TUser user, string email, string subject, string content)
    {
        using MailMessage mailMessage = new()
        {
            From = new(_options.Email, _options.DisplayedName),
            Subject = subject,
            Body = content
        };
        mailMessage.To.Add(new MailAddress(email));

        ISmtpClient smtpClient = await _clientFactory.GetSmtpClientAsync();
        await smtpClient.SendAsync(MimeMessage.CreateFromMailMessage(mailMessage));

        return IdentityResult.Success;
    }
}
