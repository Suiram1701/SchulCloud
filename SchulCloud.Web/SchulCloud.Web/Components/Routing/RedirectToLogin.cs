using Microsoft.AspNetCore.Components;
using SchulCloud.Web.Extensions;

namespace SchulCloud.Web.Components.Routing;

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
