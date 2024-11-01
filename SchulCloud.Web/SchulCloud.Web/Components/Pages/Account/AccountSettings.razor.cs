using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Web.Components.Pages.Account;

[Route("/account/settings")]
public sealed partial class AccountSettings : ComponentBase
{
    #region
    [Inject]
    private IStringLocalizer<AccountSettings> Localizer { get; set; } = default!;
    #endregion
}
