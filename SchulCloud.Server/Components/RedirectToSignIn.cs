using Microsoft.AspNetCore.Components;
using SchulCloud.Server.Extensions;

namespace SchulCloud.Server.Components;

public class RedirectToSignIn : ComponentBase
{
    #region Injections
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    #endregion

    protected override void OnInitialized()
    {
        NavigationManager.NavigateToSignIn(returnUrl: NavigationManager.GetRelativeUri());
    }
}
