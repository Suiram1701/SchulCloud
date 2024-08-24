using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Web.Components.Layouts;

public sealed partial class AccountSecurityLayout : LayoutComponentBase
{
    #region Injection
    [Inject]
    private IStringLocalizer<AccountSecurityLayout> Localizer { get; set; } = default!;
    #endregion
}
