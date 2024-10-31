using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Web.Components.Layouts.PageLayouts;

public sealed partial class AccountSecurityLayout : LayoutComponentBase
{
    #region Injection
    [Inject]
    private IStringLocalizer<AccountSecurityLayout> Localizer { get; set; } = default!;

    [Inject]
    private AppUserManager UserManager { get; set; } = default!;
    #endregion

}