using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using SchulCloud.Server.Options;

namespace SchulCloud.Server.Components;

/// <summary>
/// Sets the page title in the default format.
/// </summary>
public partial class SchulCloudPageTitle : ComponentBase
{
    #region Injections
    [Inject]
    private IOptionsSnapshot<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;
    #endregion

    [Parameter]
    public required string Title { get; set; }
}
