using Microsoft.AspNetCore.Identity;

namespace SchulCloud.Web.Extensions;

public static class UserManagerExtensions
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
    public static async Task<string> GetAnonymizedEmailAsync<TUser>(this UserManager<TUser> manager, TUser user)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(user);

        string? userEmail = await manager.GetEmailAsync(user).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            throw new InvalidOperationException("The user doesn't have an email address.");
        }

        int atIndex = userEmail.IndexOf('@');
        if (atIndex <= -1)
        {
            throw new InvalidOperationException("The user doesn't have a valid email address.");
        }

        string localPart = userEmail[..atIndex];
        string domainPart = userEmail[atIndex..];

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
