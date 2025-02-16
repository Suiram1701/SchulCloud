﻿namespace SchulCloud.Frontend.Identity.EmailSenders;

/// <summary>
/// An interface that provides sending automated emails.
/// </summary>
/// <typeparam name="TUser"></typeparam>
public interface IEmailSender<TUser>
    where TUser : class
{
    /// <summary>
    /// Sends a email confirm link to the specified <paramref name="email"/> address.
    /// </summary>
    /// <param name="user">The user that triggered the email.</param>
    /// <param name="email">The recipient email.</param>
    /// <param name="confirmLink">The reset link.</param>
    /// <returns></returns>
    public Task SendEmailConfirmLinkAsync(TUser user, string email, string confirmLink);

    /// <summary>
    /// Sends a password reset email to the specified <paramref name="email"/> address.
    /// </summary>
    /// <remarks>
    /// If an error happens an exception will be thrown.
    /// </remarks>
    /// <param name="user">The user that triggered the email.</param>
    /// <param name="email">The recipient email.</param>
    /// <param name="resetLink">The reset link.</param>
    public Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink);

    /// <summary>
    /// Sens a two factor authentication code to the specified <paramref name="email"/>.
    /// </summary>
    /// <remarks>
    /// If an error happens an exception will be thrown.
    /// </remarks>
    /// <param name="user">The user that triggered the email.</param>
    /// <param name="email">The recipient email.</param>
    /// <param name="code">The authentication code.</param>
    /// <returns></returns>
    public Task Send2faEmailCodeAsync(TUser user, string email, string code);
}
