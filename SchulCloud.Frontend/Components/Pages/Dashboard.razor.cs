using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Frontend.Components.Pages;

[Route("/")]
public sealed partial class Dashboard : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<Dashboard> Localizer { get; set; } = default!;
    #endregion
}
