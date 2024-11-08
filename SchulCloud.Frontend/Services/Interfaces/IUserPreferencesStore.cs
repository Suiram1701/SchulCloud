using Microsoft.AspNetCore.Localization;
using SchulCloud.Frontend.Enums;

namespace SchulCloud.Frontend.Services.Interfaces;

/// <summary>
/// A service interface that provides methods to store anonymous user's preferences.
/// </summary>
public interface IUserPreferencesStore
{
    /// <summary>
    /// Gets the preferred cultures of the user.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The cultures. If <c>null</c> no preferred cultures were found.</returns>
    public Task<RequestCulture?> GetPreferredCulturesAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the preferred cultures of the user.
    /// </summary>
    /// <param name="culture">The new cultures to use. If <c>null</c> the preferred cultures will be cleared.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetPreferredCulturesAsync(RequestCulture? culture, CancellationToken ct = default);

    /// <summary>
    /// Gets the preferred color theme of the user.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The color theme. If no theme were found <see cref="ColorTheme.Auto"/> will be used.</returns>
    public Task<ColorTheme> GetPreferredColorThemeAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the preferred color theme of the user.
    /// </summary>
    /// <param name="theme">The new theme to use.</param>
    /// <param name="ct">Cancellation token</param>
    public Task SetPreferredColorThemeAsync(ColorTheme theme, CancellationToken ct = default);
}
