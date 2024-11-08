using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Frontend.Components.Pages.Account;

[Route("/account/settings")]
public sealed partial class AccountSettings : ComponentBase
{
    #region
    [Inject]
    private IStringLocalizer<AccountSettings> Localizer { get; set; } = default!;
    #endregion
}
