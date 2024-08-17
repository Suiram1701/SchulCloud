using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Web.Components.Account;

public partial class SecurityMethodItemTitle : ComponentBase
{
    #region Injection
    [Inject]
    private IStringLocalizer<SecurityMethodItemTitle> Localizer { get; set; } = default!;
    #endregion

    /// <summary>
    /// The google font icon name of the icon this item has.
    /// </summary>
    [Parameter]
    public string? IconName { get; set; }

    /// <summary>
    /// The displayed title of the item.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The datetime where this method was used last time.
    /// </summary>
    [Parameter]
    public DateTime? LastTimeUsed { get; set; }

    /// <summary>
    /// Indicates whether the enabled/disabled badge should be shown.
    /// </summary>
    /// <remarks>
    /// If <c>false</c> disabled will be shown. If <c>true</c> enabled will be shown. If <c>null</c> nothing will be shown.
    /// </remarks>
    [Parameter]
    public bool? Enabled { get; set; }

    /// <summary>
    /// This fragment will be placed after the enabled/disabled badge and should be used for more badges.
    /// </summary>
    [Parameter]
    public RenderFragment? AdditionalBadges { get; set; }
}
