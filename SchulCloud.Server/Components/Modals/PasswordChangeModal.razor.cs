using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using SchulCloud.Database.Models;
using SchulCloud.Server.Extensions;
using SchulCloud.Server.Models;

namespace SchulCloud.Server.Components.Modals;

public sealed partial class PasswordChangeModal : ComponentBase, IAsyncDisposable
{
    #region Injections
    [Inject]
    private IStringLocalizer<PasswordChangeModal> Localizer { get; set; } = default!;

    [Inject]
    private IPasswordValidator<User> PasswordValidator { get; set; } = default!;

    [Inject]
    private UserManager<User> UserManager { get; set; } = default!;

    [Inject]
    private IdentityErrorDescriber ErrorDescriber { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;
    #endregion

    private EditContext _editContext = default!;

    private User _user = default!;

    public Modal Modal { get; set; } = default!;

    public PasswordChangeModel Model { get; set; } = new();

    [CascadingParameter]
    public Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override void OnInitialized()
    {
        _editContext = new(Model);
    }

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState state = await AuthenticationState.ConfigureAwait(false);
        _user = (await UserManager.GetUserAsync(state.User).ConfigureAwait(false))!;
    }

    private async Task<IEnumerable<string>> ValidateCurrentPasswordAsync(EditContext context, FieldIdentifier identifier)
    {
        if (!await UserManager.CheckPasswordAsync(_user, Model.CurrentPassword).ConfigureAwait(false))
        {
            return [ErrorDescriber.PasswordMismatch().Description];
        }

        return [];
    }

    private async Task<IEnumerable<string>> ValidateNewPasswordAsync(EditContext context, FieldIdentifier identifier)
    {
        IdentityResult result = await PasswordValidator.ValidateAsync(UserManager, _user, Model.NewPassword).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return result.Errors.Select(err => err.Description);
        }

        return [];
    }

    private Task<IEnumerable<string>> ValidateConfirmedPasswordAsync(EditContext context, FieldIdentifier identifier)
    {
        if (!Model.NewPassword.Equals(Model.ConfirmedPassword))
        {
            return Task.FromResult<IEnumerable<string>>([Localizer["confirmedPassword_DoesNotMatch"].Value]);
        }

        return Task.FromResult<IEnumerable<string>>([]);
    }

    private async Task CloseModal_ClickAsync()
    {
        await Modal.HideAsync().ConfigureAwait(false);
    }

    private async Task ChangePassword_ClickAsync()
    {
        if (!_editContext.Validate())
        {
            return;
        }

        IdentityResult result = await UserManager.ChangePasswordAsync(_user, Model.CurrentPassword, Model.NewPassword).ConfigureAwait(true);     // The original context is required here because the ToastService needs a synchronized context.
        if (result.Succeeded)
        {
            Model = new();

            ToastService.NotifySuccess(Localizer["successToast_Title"], Localizer["successToast_Message"]);
            await Modal.HideAsync().ConfigureAwait(false);
        }
        else
        {
            ToastService.NotifyError(result.Errors, Localizer["errorToast_Title"]);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Modal.DisposeAsync().ConfigureAwait(false);
    }
}
