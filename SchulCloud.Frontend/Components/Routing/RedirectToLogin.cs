using Microsoft.AspNetCore.Components;
using SchulCloud.Frontend.Extensions;

namespace SchulCloud.Frontend.Components.Routing;

public class RedirectToLogin : ComponentBase
{
    #region Injections
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    #endregion

    protected override void OnInitialized()
    {
        NavigationManager.NavigateToLogin(returnUrl: NavigationManager.GetRelativeUri());
    }
}
