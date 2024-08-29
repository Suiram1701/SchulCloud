using BlazorBootstrap;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using SchulCloud.Store.Managers;
using SchulCloud.Web.Components.Dialogs;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Models;
using SchulCloud.Web.Services;
using SchulCloud.Web.Services.EventArgs;

namespace SchulCloud.Web.Components.Pages.Account.Security;

[StreamRendering(true)]
[Route("/account/security/securityKeys")]
public sealed partial class SecurityKeys : ComponentBase, IAsyncDisposable
{
    #region Injections
    [Inject]
    private IStringLocalizer<SecurityKeys> Localizer { get; set; } = default!;

    [Inject]
    private SchulCloudUserManager<ApplicationUser, AppCredential> UserManager { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private WebAuthnService WebAuthnService { get; set; } = default!;
    #endregion

    private Modal _registerModal = default!;
    private RenameDialog _renameDialog = default!;
    private RemoveDialog _removeDialog = default!;

    private bool _webAuthnSupported = true;
    private ApplicationUser _user = default!;
    private List<SecurityKey>? _securityKeys;

    private readonly RegisterFido2CredentialModel _registerModel = new();
    private IAsyncDisposable? _pendingRegistration;
    private RegisterState? _registerState;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState.ConfigureAwait(false);
        _user = (await UserManager.GetUserAsync(authenticationState.User).ConfigureAwait(false))!;

        _securityKeys ??= [];
        foreach (AppCredential credential in await UserManager.GetFido2CredentialsByUserAsync(_user).ConfigureAwait(false))
        {
            _securityKeys.Add(await CredentialToSecurityKeyAsync(credential).ConfigureAwait(false));
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        if (!await WebAuthnService.IsSupportedAsync().ConfigureAwait(false))
        {
            _webAuthnSupported = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private async Task RegisterSecurityKey_ClickAsync()
    {
        if (_webAuthnSupported)
        {
            await _registerModal.ShowAsync().ConfigureAwait(false);
        }
    }

    private async Task RegisterModalClose_ClickAsync()
    {
        await _registerModal.HideAsync().ConfigureAwait(false);
    }

    private async Task RegisterModal_ValidSubmitAsync()
    {
        if (_pendingRegistration is not null)
        {
            await _pendingRegistration.DisposeAsync().ConfigureAwait(false);
        }

        CredentialCreateOptions options = await UserManager.CreateFido2CreationOptionsAsync(_user, _registerModel.IsPasskey).ConfigureAwait(false);

        _registerState = new(_registerModel.SecurityKeyName, _registerModel.IsPasskey, options);
        _pendingRegistration = await WebAuthnService.StartCreateCredentialAsync(options, OnCredentialRegistrationCompletedAsync).ConfigureAwait(false);
    }

    private async void OnCredentialRegistrationCompletedAsync(object? sender, WebAuthnCompletedEventArgs<AuthenticatorAttestationRawResponse> args)
    {
        _pendingRegistration = null;
        if (_registerState is null)
        {
            return;
        }

        if (!args.Successful)
        {
            await InvokeAsync(() =>
            {
                ToastService.NotifyError(Localizer["registerModal_Error"], args.ErrorMessage ?? Localizer["registerModal_ErrorMessage"]);
            }).ConfigureAwait(false);
            return;
        }

        IdentityResult result = await UserManager.StoreFido2CredentialsAsync(_user, _registerState.SecurityKeyName, _registerState.IsPasskey, args.Result, _registerState.Options).ConfigureAwait(false);
        if (result.Succeeded)
        {
            _registerState = null;

            AppCredential credential = (await UserManager.GetFido2CredentialById(args.Result.Id).ConfigureAwait(false))!;
            SecurityKey securityKey = await CredentialToSecurityKeyAsync(credential).ConfigureAwait(false);

            _securityKeys ??= [];
            _securityKeys.Add(securityKey);
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            
            await _registerModal.HideAsync().ConfigureAwait(false);
        }
        else
        {
            await InvokeAsync(() => ToastService.NotifyError(result.Errors, Localizer["registerModal_SaveError"])).ConfigureAwait(false);
        }
    }

    private async Task Remove_ClickAsync(SecurityKey securityKey)
    {
        if (!await _removeDialog.ShowAsync(Localizer["removeDialog"], Localizer["removeDialogMessage"]).ConfigureAwait(false))
        {
            return;
        }

        IdentityResult result = await UserManager.RemoveFido2CredentialAsync(securityKey.Credential, _user).ConfigureAwait(false);
        if (result.Succeeded)
        {
            _securityKeys?.Remove(securityKey);
        }
        else
        {
            await InvokeAsync(() => ToastService.NotifyError(result.Errors, Localizer["removeError"])).ConfigureAwait(false);
        }
    }

    private async Task SecurityKeyChangeName_ClickAsync(SecurityKey securityKey)
    {
        string? newName = await _renameDialog.ShowAsync(
            oldName: securityKey.Name,
            excludedNames: _securityKeys?.Select(key => key.Name) ?? [],
            title: Localizer["securityKey_rename"]).ConfigureAwait(false);
        if (string.IsNullOrEmpty(newName))
        {
            return;
        }

        IdentityResult renameResult = await UserManager.ChangeFido2CredentialSecurityKeyNameAsync(securityKey.Credential, _user, newName).ConfigureAwait(false);
        if (renameResult.Succeeded)
        {
            securityKey.Name = newName;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
        else
        {
            await InvokeAsync(() => ToastService.NotifyError(renameResult.Errors, Localizer["securityKey_renameError"])).ConfigureAwait(false);
        }
    }

    private Task<IEnumerable<string>> ValidateSecurityKeyNameAsync(EditContext context, FieldIdentifier identifier)
    {
        string? errorMessage = null;
        if (string.IsNullOrWhiteSpace(_registerModel.SecurityKeyName))
        {
            errorMessage = Localizer["registerModal_keyName_notEmpty"];
        }
        else if (_registerModel.SecurityKeyName.Length > 256)
        {
            errorMessage = Localizer["registerModal_keyName_toLarge"];
        }
        else
        {
            foreach (string? credName in _securityKeys?.Select(key => key.Name) ?? [])
            {
                if (_registerModel.SecurityKeyName.Equals(credName))
                {
                    errorMessage = Localizer["registerModal_keyName_alreadyExists"];
                }
            }
        }

        IEnumerable<string> messages = !string.IsNullOrEmpty(errorMessage)
            ? [errorMessage]
            : [];
        return Task.FromResult(messages);
    }

    private async Task<SecurityKey> CredentialToSecurityKeyAsync(AppCredential credential)
    {
        string? keyName = await UserManager.GetFido2CredentialSecurityKeyNameAsync(credential).ConfigureAwait(false);
        bool isPasskey = await UserManager.GetFido2CredentialIsPasskey(credential).ConfigureAwait(false);
        AuthenticatorTransport[]? transports = await UserManager.GetFido2CredentialTransportsAsync(credential).ConfigureAwait(false);
        DateTime registrationDate = await UserManager.GetFido2CredentialRegistrationDateAsync(credential).ConfigureAwait(false);
        MetadataStatement? metadata = await UserManager.GetFido2CredentialMetadataStatementAsync(credential).ConfigureAwait(false);

        return new(credential, keyName, isPasskey, transports, registrationDate, metadata);
    }

    public async ValueTask DisposeAsync()
    {
        if (_pendingRegistration is not null)
        {
            await _pendingRegistration.DisposeAsync().ConfigureAwait(false);
        }
    }

    private record SecurityKey(AppCredential Credential, string? Name, bool IsPasskey, AuthenticatorTransport[]? Transports, DateTime RegistrationDate, MetadataStatement? Metadata)
    {
        public string? Name { get; set; } = Name;
    }

    private record RegisterState(string SecurityKeyName, bool IsPasskey, CredentialCreateOptions Options);
}
