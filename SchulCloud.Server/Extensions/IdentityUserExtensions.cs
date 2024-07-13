using Microsoft.AspNetCore.Identity;

namespace SchulCloud.Server.Extensions;

public static class IdentityUserExtensions
{
    /// <summary>
    /// Anonymizes the user's email address by replacing the local part with *.
    /// </summary>
    /// <remarks>
    /// If the user doesn't have an email address an exception will be thrown.
    /// </remarks>
    /// <param name="user">The user</param>
    /// <returns>The anonymized email address.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static string GetAnonymizedEmail(this IdentityUser user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        if (user.Email is null)
        {
            throw new ArgumentException("The specified user doesn't have an email address.");
        }

        int atIndex = user.Email.IndexOf('@');
        if (atIndex <= -1)
        {
            throw new ArgumentException("Invalid email address.");
        }

        string localPart = user.Email[..atIndex];
        string domainPart = user.Email[atIndex..];

        if (localPart.Length <= 1)
        {
            return "*" + domainPart;
        }
        else
        {
            string blurredChars = string.Concat(Enumerable.Repeat('*', localPart.Length - 1));
            return localPart[0] + blurredChars + domainPart;
        }
    }
}
