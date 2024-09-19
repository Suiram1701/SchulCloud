using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using SchulCloud.Store.Managers;

namespace SchulCloud.Web.Components.Layouts.PageLayouts;

public sealed partial class AccountSecurityLayout : LayoutComponentBase
{
    #region Injection
    [Inject]
    private IStringLocalizer<AccountSecurityLayout> Localizer { get; set; } = default!;

    [Inject]
    private SchulCloudUserManager<ApplicationUser, AppCredential> UserManager { get; set; } = default!;
    #endregion

    private static string ActiveNavLinkClasses => "border-solid border-b-2 mud-border-primary mud-primary-text";
}
