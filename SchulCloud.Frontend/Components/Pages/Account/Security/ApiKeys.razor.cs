using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MudBlazor;
using SchulCloud.Frontend.Options;
using SchulCloud.Identity.Models;

namespace SchulCloud.Frontend.Components.Pages.Account.Security;

[Route("/account/security/apiKeys")]
public sealed partial class ApiKeys : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<ApiKeys> Localizer { get; set; } = default!;

    [Inject]
    private IOptionsSnapshot<ApiOptions> ApiOptionsSnapshoot { get; set; } = default!;

    [Inject]
    private ApplicationUserManager UserManager { get; set; } = default!;
    #endregion

    private IEnumerable<UserApiKey> _apiKeys = [];

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState;
        ApplicationUser user = (await UserManager.GetUserAsync(authenticationState.User))!;

        _apiKeys = await UserManager.GetApiKeysByUserAsync(user);
    }
}
