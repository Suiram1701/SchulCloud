using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace SchulCloud.Web.Components.Pages;

[Route("/")]
public sealed partial class Index : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<Index> Localizer { get; set; } = default!;
    #endregion
}
