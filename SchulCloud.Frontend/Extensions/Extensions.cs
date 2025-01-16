using Microsoft.AspNetCore.Identity;
using SchulCloud.Frontend.Options;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static SchulCloud.Frontend.Options.PresentationOptions;

namespace SchulCloud.Frontend.Extensions;

/// <summary>
/// A class that contains some small extensions.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Returns the best available favicon.
    /// </summary>
    /// <remarks>
    /// As the best favicon will be the first one with the size <c>any</c> interpreted (this might be an svg). 
    /// If no fav. with size <c>any</c> is found the one with the highest resolution will be returned.
    /// </remarks>
    /// <returns>The favicon. If <c>null</c> no favicon at all is available.</returns>
    public static Favicon? GetBestFavicon(this PresentationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Favicons.FirstOrDefault(fav => fav.Sizes == "any") is Favicon anyFav)
        {
            return anyFav;
        }

        Favicon? resolutionFav = options.Favicons
            .Where(fav => !string.IsNullOrEmpty(fav.Sizes))
            .Select(fav => (size: int.Parse(fav.Sizes![..fav.Sizes!.IndexOf('x')]), fav))
            .OrderByDescending(item => item.size)
            .FirstOrDefault()
            .fav;
        return resolutionFav ?? options.Favicons.FirstOrDefault();
    }

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

        string? userEmail = await manager.GetEmailAsync(user);
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

    /// <summary>
    /// Indicates whether a http context is valid.
    /// </summary>
    /// <param name="context">The context to verify.</param>
    /// <returns>The result.</returns>
    public static bool IsContextValid([NotNullWhen(true)] this HttpContext? context)
    {
        if (context is null)
        {
            return false;
        }

        return !context.WebSockets.IsWebSocketRequest;
    }

    /// <summary>
    /// Formats the datetime to a string displayed to the user.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ToDisplayedString(this DateTime dateTime)
    {
        return dateTime.ToString("d", CultureInfo.CurrentUICulture);
    }
}
