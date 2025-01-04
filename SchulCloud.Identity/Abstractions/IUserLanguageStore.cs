using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Identity.Abstractions;

/// <summary>
/// A store interface which provides methods to get and set a user's language.
/// </summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public interface IUserLanguageStore<TUser>
    where TUser : class
{
    /// <summary>
    /// Sets the culture of a certain user in which data will be shown.
    /// </summary>
    /// <param name="user">The user to set the culture for.</param>
    /// <param name="culture">The new culture to set.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetCultureAsync(TUser user, CultureInfo? culture, CancellationToken ct);

    /// <summary>
    /// Sets the UI culture of a certain user.
    /// </summary>
    /// <param name="user">The user to set the UI culture for.</param>
    /// <param name="culture">The new culture to set.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetUiCultureAsync(TUser user, CultureInfo? culture, CancellationToken ct);

    /// <summary>
    /// Gets the culture of a certain user in which data will be shown.
    /// </summary>
    /// <param name="user">The user to get the culture for.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The culture. If <c>null</c> it wasn't set before.</returns>
    public Task<CultureInfo?> GetCultureAsync(TUser user, CancellationToken ct);

    /// <summary>
    /// Gets the UI culture of a certain user.
    /// </summary>
    /// <param name="user">The user to get the UI culture for.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The culture. If <c>null</c> it wasn't set before.</returns>
    public Task<CultureInfo?> GetUiCultureAsync(TUser user, CancellationToken ct);
}
