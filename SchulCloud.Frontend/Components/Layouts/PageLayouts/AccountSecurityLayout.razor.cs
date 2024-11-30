using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Identity.Options;

namespace SchulCloud.Frontend.Components.Layouts.PageLayouts;

public sealed partial class AccountSecurityLayout : LayoutComponentBase
{
    #region Injection
    [Inject]
    private IStringLocalizer<AccountSecurityLayout> Localizer { get; set; } = default!;

    [Inject]
    private IOptionsSnapshot<ApiKeyOptions> ApiKeyOptionsAccessor { get; set; } = default!;

    [Inject]
    private ApplicationUserManager UserManager { get; set; } = default!;
    #endregion
}