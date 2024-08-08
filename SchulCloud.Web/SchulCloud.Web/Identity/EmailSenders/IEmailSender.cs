using Microsoft.AspNetCore.Identity;

namespace SchulCloud.Web.Identity.EmailSenders;

/// <summary>
/// An interface that provides sending automated emails.
/// </summary>
/// <typeparam name="TUser"></typeparam>
public interface IEmailSender<TUser>
    where TUser : IdentityUser
{
    /// <summary>
    /// Sends a password reset email to the specified <paramref name="email"/> address.
    /// </summary>
    /// <remarks>
    /// If an error happens an exception will be thrown.
    /// </remarks>
    /// <param name="user">The user that issued this.</param>
    /// <param name="email">The recipient email.</param>
    /// <param name="resetLink">The reset link.</param>
    public Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink);
}
