using SchulCloud.Identity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Identity.Abstractions;

/// <summary>
/// A store interface which provides methods to set or get a user's color themes.
/// </summary>
/// <typeparam name="TUser">The type of user.</typeparam>
public interface IUserColorThemeStore<TUser>
    where TUser : class
{
    /// <summary>
    /// Sets the color theme for a certain user.
    /// </summary>
    /// <param name="user">The user to set the theme for.</param>
    /// <param name="theme">The theme to set.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A task to await the asynchronous operation.</returns>
    public Task SetColorThemeAsync(TUser user, ColorTheme? theme, CancellationToken ct);

    /// <summary>
    /// Gets the color theme of a certain user.
    /// </summary>
    /// <param name="user">The user to get the theme from.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The theme. If <c>null</c> the color theme were not set.</returns>
    public Task<ColorTheme?> GetColorThemeAsync(TUser user, CancellationToken ct);
}
