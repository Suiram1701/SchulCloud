using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using SchulCloud.ServiceDefaults.Options;

namespace SchulCloud.Frontend.Components;

public sealed partial class App : ComponentBase
{
    #region Injections
    [Inject]
    private IOptionsSnapshot<ServiceOptions> OptionsAccessor { get; set; } = default!;
    #endregion
}
