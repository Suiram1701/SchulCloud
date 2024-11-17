using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using SchulCloud.Authorization;
using SchulCloud.Frontend.Components.Dialogs;
using SchulCloud.Frontend.Extensions;
using SchulCloud.Frontend.Models;
using SchulCloud.Store.Models;
using System.Text;

namespace SchulCloud.Frontend.Components.Pages.Account.Security;

[Route("/account/security/apiKeys/create")]
public sealed partial class ApiKeyCreate : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<ApiKeyCreate> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private AppUserManager UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    #endregion

    private ApplicationUser _user = default!;
    private IReadOnlyCollection<Permission> _userPermissions = [];

    private MudForm _createForm = default!;
    private bool _formIsValid;
    private readonly CreateApiKeyModel _createKeyModel = new();

    private MudDialog _showKeyDialog = default!;
    private string? _apiKey;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState;
        _user = (await UserManager.GetUserAsync(authenticationState.User))!;
        _userPermissions = await UserManager.GetPermissionLevelsAsync(_user);
    }

    private async Task<bool> CheckMinimumPermissionsAsync()
    {
        if (!_createKeyModel.AllPermissions
            && (_createKeyModel.PermissionLevels.Count == 0
            || _createKeyModel.PermissionLevels.All(p => p.Value == PermissionLevel.None)))
        {
            await DialogService.ShowMessageBox(new()
            {
                Title = Localizer["noPermissions"],
                Message = Localizer["noPermissions_Message"],
                YesText = Localizer["noPermissions_YesBtn"],
            });
            return false;
        }

        return true;
    }

    private (PermissionLevel level, EventCallback<int> changedCallback) GetPermissionLevel(Permission userPermission)
    {
        if (_createKeyModel.AllPermissions)
        {
            return (userPermission.Level, EventCallback<int>.Empty);
        }
        else
        {
            if (!_createKeyModel.PermissionLevels.TryGetValue(userPermission.Name, out PermissionLevel keyLevel))
            {
                keyLevel = PermissionLevel.None;
            }
            EventCallback<int> changedCallback = EventCallback.Factory.Create<int>(this, value =>
            {
                PermissionLevel level = (PermissionLevel)value;
                if (level != PermissionLevel.None)
                {
                    _createKeyModel.PermissionLevels[userPermission.Name] = level;
                }
                else
                {
                    _createKeyModel.PermissionLevels.Remove(userPermission.Name);
                }
            });

            return (keyLevel, changedCallback);
        }
    }

    private async Task<string?> Name_ValidateAsync(string value)
    {
        UserApiKey[] keys = await UserManager.GetApiKeysByUserAsync(_user);
        return keys.Any(key => key.Name == value)
            ? Localizer["form_Name_MustUnique"].ToString()
            : null;
    }

    private string? Expires_Validate(DateTime? value)
    {
        return value is not null && value <= DateTime.Now
            ? Localizer["form_Expires_MustInFuture"].ToString()
            : null;
    }

    private void ExpiresClear_Click()
    {
        _createKeyModel.Expires = null;
        StateHasChanged();
    }

    private async Task Create_ClickAsync()
    {
        if (!await CheckMinimumPermissionsAsync())
        {
            return;
        }

        await _createForm.Validate();
        if (_createForm.IsValid)
        {
            if (_createKeyModel.Expires is null)
            {
                IDialogReference confirmRef = await DialogService.ShowConfirmDialogAsync(
                    title: Localizer["confirmNeverExpire"],
                    message: Localizer["confirmNeverExpire_Message"],
                    confirmColor: Color.Warning);
                if (!(await confirmRef.GetReturnValueAsync<bool?>() ?? false))
                {
                    return;
                }
            }

            UserApiKey key = new()
            {
                Name = _createKeyModel.Name,
                Notes = _createKeyModel.Notes,
                Expiration = _createKeyModel.Expires?.ToUniversalTime(),
                AllPermissions = _createKeyModel.AllPermissions,
                PermissionLevels = _createKeyModel.PermissionLevels.Select(kv => new Permission(kv.Key, kv.Value)).ToHashSet(),
            };

            _apiKey = await UserManager.AddApiKeyToUserAsync(_user, key);
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                UserApiKey createdKey = (await UserManager.GetApiKeysByUserAsync(_user)).First(key => key.Name == _createKeyModel.Name);

                IDialogReference dialogRef = await _showKeyDialog.ShowAsync();
                await dialogRef.Result;

                NavigationManager.NavigateToApiKeyDetails(createdKey.Id);
            }
            else
            {
                SnackbarService.AddError(Localizer["createKey_Error"]);
            }
        }
    }

    private async Task ShowKeyDialog_DownloadKey_ClickAsync()
    {
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(_apiKey));
            await JSRuntime.DownloadFileAsync(stream, _createKeyModel.Name, mimeType: "text/plain");
        }
    }
}
