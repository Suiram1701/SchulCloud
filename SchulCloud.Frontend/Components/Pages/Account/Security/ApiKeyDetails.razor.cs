using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using MudBlazor;
using SchulCloud.Authorization;
using SchulCloud.Frontend.Components.Dialogs;
using SchulCloud.Frontend.Extensions;
using SchulCloud.Store.Models;

namespace SchulCloud.Frontend.Components.Pages.Account.Security;

[Route("/account/security/apiKeys/{apiKeyId}")]
public sealed partial class ApiKeyDetails
{
    #region Injections
    [Inject]
    private IStringLocalizer<ApiKeyDetails> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private AppUserManager UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    #endregion

    private bool _loaded;

    private ApplicationUser _user = default!;
    private IReadOnlyDictionary<string, PermissionLevel> _userPermissions = default!;

    private bool _authorizedAccess;
    private UserApiKey? _apiKey;

    [Parameter]
    public string ApiKeyId { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _apiKey = await UserManager.FindApiKeyById(ApiKeyId);
        if (_apiKey is not null)
        {
            AuthenticationState authenticationState = await AuthenticationState;
            _user = (await UserManager.FindUserByApiKeyAsync(_apiKey))!;
            _authorizedAccess = UserManager.GetUserId(authenticationState.User) == await UserManager.GetUserIdAsync(_user);

            _userPermissions = await UserManager.GetPermissionLevelsAsync(_user);
        }

        _loaded = true;
        StateHasChanged();
    }

    private async Task Remove_ClickAsync()
    {
        if (_apiKey is not null)
        {
            IDialogReference dialogRef = await DialogService.ShowConfirmDialogAsync(Localizer["removeKeyBtn"], Localizer["removeKey_Desc"], confirmColor: Color.Error);
            if (await dialogRef.GetReturnValueAsync<bool?>() ?? false)
            {
                IdentityResult removeResult = await UserManager.RemoveApiKeyAsync(_user, _apiKey);
                if (removeResult.Succeeded)
                {
                    Snackbar.AddSuccess(Localizer["removeKey_Success"]);
                    NavigationManager.NavigateToApiKeys();
                }
                else
                {
                    Snackbar.AddError(removeResult.Errors, Localizer["removeKey_Error"]);
                }
            }
        }
    }
}
