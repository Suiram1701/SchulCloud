using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Web.Components.Pages.Account;

[Route("/account/settings")]
public sealed partial class Settings : ComponentBase
{
    #region
    [Inject]
    private IStringLocalizer<Settings> Localizer { get; set; } = default!;
    #endregion
}
