using Microsoft.AspNetCore.Components;

namespace SchulCloud.Web.Components;

public sealed partial class App : ComponentBase
{
    #region Inject
    [Inject]
    private IHostEnvironment Environment { get; set; } = default!;
    #endregion
}
