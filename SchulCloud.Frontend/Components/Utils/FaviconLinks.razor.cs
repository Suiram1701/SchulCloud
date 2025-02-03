using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using SchulCloud.Frontend.Options;

namespace SchulCloud.Frontend.Components.Utils;

public partial class FaviconLinks : ComponentBase
{
    #region Injections
    [Inject]
    private IOptions<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;
    #endregion
}
