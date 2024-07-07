using Microsoft.AspNetCore.Identity;
using SchulCloud.Database.Models;

namespace SchulCloud.Server.Identity.EmailSenders;

/// <summary>
/// An email sender implementation that prints the mails into the logs.
/// </summary>
/// <remarks>
/// This implementation should only be used in development environment.
/// </remarks>
public partial class DevEmailSender(IHostEnvironment environment, ILogger<DevEmailSender> logger) : EmailSenderBase(logger)
{
    private readonly IHostEnvironment _environment = environment;

    public override Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        using IDisposable? loggerScope = OpenLoggerScope(user, email);
        CheckIfDevelopment();

        LogSentEmail(_logger, user.UserName!, user.Id, email, $"Account confirmation {confirmationLink}");
        return Task.CompletedTask;
    }

    public override Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        using IDisposable? loggerScope = OpenLoggerScope(user, email);
        CheckIfDevelopment();

        LogSentEmail(_logger, user.UserName!, user.Id, email, $"Password reset code {resetCode}");
        return Task.CompletedTask;
    }

    public override Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        using IDisposable? loggerScope = OpenLoggerScope(user, email);
        CheckIfDevelopment();

        LogSentEmail(_logger, user.UserName!, user.Id, email, $"Password reset link {resetLink}");
        return Task.CompletedTask;
    }

    private void CheckIfDevelopment()
    {
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("{type} designed to handle emails only in the {devEnv}. Current environment: {env}.", typeof(DevEmailSender), Environments.Development, _environment.EnvironmentName);
        }
    }

    protected override Task<IdentityResult> ExecuteAsync(User user, string email, string subject, string content)
    {
        CheckIfDevelopment();

        LogSentEmail(_logger, user.UserName!, user.Id, email, $"Subject: {subject}; Raw content: {content}");
        return Task.FromResult(IdentityResult.Success);
    }

    [LoggerMessage(LogLevel.Trace, "Email to {email} issued by {userName} ({userId}): {purpose}")]
    private static partial void LogSentEmail(ILogger logger, string userName, string userId, string email, string purpose);
}