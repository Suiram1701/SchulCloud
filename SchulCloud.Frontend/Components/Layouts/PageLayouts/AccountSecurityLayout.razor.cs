using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Frontend.Components.Layouts.PageLayouts;

public sealed partial class AccountSecurityLayout : LayoutComponentBase
{
    #region Injection
    [Inject]
    private IStringLocalizer<AccountSecurityLayout> Localizer { get; set; } = default!;

    [Inject]
    private ApplicationUserManager UserManager { get; set; } = default!;
    #endregion

}