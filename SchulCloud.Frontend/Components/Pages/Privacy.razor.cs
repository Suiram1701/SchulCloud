using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Frontend.Components.Pages;

[AllowAnonymous]
[Route("/privacy")]
public sealed partial class Privacy : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<Privacy> Localizer { get; set; } = default!;
    #endregion
}
