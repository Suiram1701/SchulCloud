using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using SchulCloud.Web.Options;

namespace SchulCloud.Web.Components;

public partial class FaviconLinks : ComponentBase
{
    #region Injections
    [Inject]
    private IOptions<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;
    #endregion
}
