using System.Collections.Immutable;

namespace SchulCloud.Server.Utils.Interfaces;

/// <summary>
/// An interface that provides a helper to use cookies via a SignalR connection.
/// </summary>
public interface ICookieHelper
{
    /// <summary>
    /// Sets a cookie with the specified settings.
    /// </summary>
    /// <param name="name">The name of the cookie.</param>
    /// <param name="value">The value.</param>
    /// <param name="options">The options of this cookie.</param>
    public ValueTask SetCookieAsync(string name, string value, CookieOptions options);

    /// <summary>
    /// Gets the value of a cookie.
    /// </summary>
    /// <remarks>
    /// When the cookie couldn't found <c>null</c> will be returned.
    /// </remarks>
    /// <param name="name">The name of the cookie</param>
    /// <returns>The value of the cookie.</returns>
    public ValueTask<string?> GetCookieAsync(string name);

    /// <summary>
    /// Gets every cookie.
    /// </summary>
    /// <returns>The cookies.</returns>
    public ValueTask<ImmutableDictionary<string, string>> GetCookiesAsync();

    /// <summary>
    /// Deletes a cookie with the specified name.
    /// </summary>
    /// <param name="name">The name of the cookie to delete.</param>
    public ValueTask RemoveCookieAsync(string name);
}
