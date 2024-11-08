using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using SchulCloud.Store.Models;
using SchulCloud.Frontend.Components.Dialogs;
using SchulCloud.Frontend.Extensions;
using SchulCloud.Frontend.Models;
using SchulCloud.Frontend.Services;
using SchulCloud.Frontend.Services.Exceptions;
using System.Globalization;
using System.Net;
using MaterialSymbols = MudBlazor.FontIcons.MaterialSymbols;

namespace SchulCloud.Frontend.Components.Pages.Account.Security;

[Route("/account/security/securityKeys")]
public sealed partial class SecurityKeys : ComponentBase, IDisposable
{
    #region Injections
    [Inject]
    private IStringLocalizer<SecurityKeys> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private AppUserManager UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private WebAuthnService WebAuthnService { get; set; } = default!;
    #endregion

    private ApplicationUser _user = default!;

    private MudDialog _registerDialog = default!;
    private MudForm _registerForm = default!;
    private RegisterSecurityKeyModel _registerModel = new();

    private List<UserCredential>? _securityKeys;
    private int _selectedPage = 1;
    private const int _keysPerPage = 10;

    private readonly HashSet<byte[]> _passkeys = [];
    private readonly Dictionary<byte[], MetadataStatement> _metadata = [];

    private bool _webAuthnSupported = true;
    private readonly CancellationTokenSource _webAuthnCts = new();

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (!UserManager.SupportsUserCredentials || !(UserManager.SupportsUserPasskeys || UserManager.SupportsUserTwoFactorSecurityKeys))
        {
            NavigationManager.NavigateToNotFound();
            return;
        }

        AuthenticationState authenticationState = await AuthenticationState;
        _user = (await UserManager.GetUserAsync(authenticationState.User))!;

        IEnumerable<UserCredential> credentials = await UserManager.FindFido2CredentialsByUserAsync(_user);
        _securityKeys = credentials.ToList();

        foreach (UserCredential credential in _securityKeys)
        {
            await AddSecurityKeyAsync(credential);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (!await WebAuthnService.IsSupportedAsync())
            {
                _webAuthnSupported = false;
                StateHasChanged();
            }
        }
    }

    private async Task RegisterSecurityKey_ClickAsync()
    {
        if (_webAuthnSupported)
        {
            await _registerDialog.ShowAsync();
        }
    }

    private async Task SecurityKeyChangeName_ClickAsync(UserCredential securityKey)
    {
        IDialogReference dialogReference = await DialogService.ShowRenameDialogAsync(
            Localizer["renameDialog"],
            null,
            oldName: securityKey.Name,
            excludedNames: _securityKeys!.Select(key => key.Name ?? string.Empty));
        DialogResult? dialogResult = await dialogReference.Result;
        if (dialogResult?.Canceled ?? true)
        {
            return;
        }

        string newName = (string)dialogResult.Data!;

        IdentityResult renameResult = await UserManager.UpdateFido2CredentialNameAsync(_user, securityKey, newName);
        if (renameResult.Succeeded)
        {
            securityKey.Name = newName;
        }
        else
        {
            SnackbarService.AddError(renameResult.Errors, Localizer["renameDialog_Error"]);
        }
    }

    private async Task SecurityKeyRemove_ClickAsync(UserCredential securityKey)
    {
        IDialogReference dialogReference = await DialogService.ShowConfirmDialogAsync(
            Localizer["removeDialog"],
            Localizer["removeDialog_Message"],
            confirmColor: Color.Error);
        if (await dialogReference.GetReturnValueAsync<bool?>() ?? false)
        {
            IdentityResult result = await UserManager.RemoveFido2CredentialAsync(securityKey, _user);
            if (result.Succeeded)
            {
                _securityKeys?.Remove(securityKey);

                if (_securityKeys?.Count == 0)     // Disable if its the last key.
                {
                    await UserManager.DisableSecurityKeyAuthenticationAsync(_user);
                }
            }
            else
            {
                SnackbarService.AddError(result.Errors, Localizer["removeDialog_Error"]);
            }
        }
    }

    private string? RegisterForm_ValidateSecurityKeyName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Localizer["form_NotEmpty"];
        }

        foreach (string? credName in _securityKeys?.Select(key => key.Name) ?? [])
        {
            if (_registerModel.SecurityKeyName.Equals(credName))
            {
                return Localizer["registerModal_KeyName_AlreadyExists"];
            }
        }

        return null;
    }

    private async Task RegisterForm_Register_ClickAsync()
    {
        await _registerForm.Validate();
        if (_registerForm.Errors.Length == 0)
        {
            CredentialCreateOptions creationOptions = await UserManager.CreateFido2CreationOptionsAsync(_user, _registerModel.IsPasskey);

            try
            {
                AuthenticatorAttestationRawResponse authenticatorResponse = await WebAuthnService.CreateCredentialAsync(creationOptions, _webAuthnCts.Token);
                await _registerDialog.CloseAsync();

                IdentityResult storeResult = await UserManager.StoreFido2CredentialsAsync(
                    _user,
                    _registerModel.SecurityKeyName,
                    _registerModel.IsPasskey,
                    authenticatorResponse,
                    creationOptions);
                if (storeResult.Succeeded)
                {
                    UserCredential credential = (await UserManager.FindFido2Credential(authenticatorResponse.Id))!;
                    (_securityKeys ??= []).Add(credential);

                    await AddSecurityKeyAsync(credential);
                    StateHasChanged();
                }
                else
                {
                    SnackbarService.AddError(storeResult.Errors, Localizer["registerDialog_SaveError"]);
                }

                _registerModel = new();
            }
            catch (WebAuthnException ex)
            {
                SnackbarService.AddError(ex.Message ?? Localizer["registerDialog_Error"]);
            }
            catch (TaskCanceledException)
            {
            }
        }
    }

    private async Task AddSecurityKeyAsync(UserCredential credential)
    {
        if (await UserManager.GetIsPasskey(credential))
        {
            _passkeys.Add(credential.Id);
        }

        MetadataStatement? metadata = await UserManager.GetFido2CredentialMetadataStatementAsync(credential);
        if (metadata is not null)
        {
            _metadata.Add(credential.Id, metadata);
        }
    }

    public void Dispose()
    {
        _webAuthnCts.Cancel();
        _webAuthnCts.Dispose();
    }

    private record RegisterState(string SecurityKeyName, bool IsPasskey, CredentialCreateOptions Options);
}
