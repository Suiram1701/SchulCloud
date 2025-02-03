using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using SchulCloud.Frontend.Options;

namespace SchulCloud.Frontend.Components.Utils;

/// <summary>
/// Sets the page title in the default format.
/// </summary>
public partial class AppPageTitle : ComponentBase
{
    #region Injections
    [Inject]
    private IOptionsSnapshot<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;
    #endregion

    [Parameter]
    public required string Title { get; set; }
}
