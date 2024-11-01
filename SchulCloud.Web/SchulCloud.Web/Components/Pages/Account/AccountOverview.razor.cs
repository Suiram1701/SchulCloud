using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Web.Components.Pages.Account;

[Route("/account")]
public sealed partial class AccountOverview : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<AccountOverview> Localizer { get; set; } = default!;
    #endregion
}