using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Frontend.Components.Pages.Account;

[Route("/account")]
public sealed partial class Overview : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<Overview> Localizer { get; set; } = default!;
    #endregion
}