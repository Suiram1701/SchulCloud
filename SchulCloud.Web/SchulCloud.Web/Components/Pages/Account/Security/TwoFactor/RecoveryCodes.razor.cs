using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Options;
using System.Text;

namespace SchulCloud.Web.Components.Pages.Account.Security.TwoFactor;

[Route("/account/security/2fa/recoveryCodes")]
public sealed partial class RecoveryCodes : ComponentBase, IDisposable
{
    #region Injections
    [Inject]
    private IStringLocalizer<RecoveryCodes> Localizer { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private IOptions<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private PersistentComponentState ComponentState { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;
    #endregion

    private ApplicationUser _user = default!;
    private string[] _recoveryCodes = [];
    private PersistingComponentStateSubscription? _stateSubscription;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState.ConfigureAwait(false);
        _user = (await UserManager.GetUserAsync(authenticationState.User).ConfigureAwait(false))!;

        if (!ComponentState.TryTakeFromJson(nameof(_recoveryCodes), out string[]? recoveryCodes))
        {
            recoveryCodes = (await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(_user, 10).ConfigureAwait(false))?.ToArray();
            if (recoveryCodes is not null)
            {
                _stateSubscription = ComponentState.RegisterOnPersisting(() =>
                {
                    ComponentState.PersistAsJson(nameof(_recoveryCodes), recoveryCodes);
                    return Task.CompletedTask;
                }, RenderMode.InteractiveServer);
            }
            else
            {
                await InvokeAsync(() => ToastService.NotifyError(Localizer["generationError_Title"], Localizer["generationError_Message"]));
            }
        }

        _recoveryCodes = recoveryCodes ?? [];
    }

    private async Task Download_ClickAsync()
    {
        if (_recoveryCodes.Length > 0)
        {
            string appName = PresentationOptionsAccessor.Value.ApplicationName;
            string userName = (await UserManager.GetUserNameAsync(_user).ConfigureAwait(false))!;
            string userEmail = (await UserManager.GetEmailAsync(_user).ConfigureAwait(false))!;

            StringBuilder fileBuilder = new();
            fileBuilder.AppendLine(Localizer["file_Introduction", appName, userName, userEmail]);
            foreach (string code in _recoveryCodes)
            {
                fileBuilder.Append("- ").AppendLine(code);
            }

            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(fileBuilder.ToString()));
            string fileName = $"{Localizer["file_Name", appName]}.txt";
            await JSRuntime.DownloadFileAsync(stream, fileName, mimeType: "text/plain", convertNlChars: true);
        }
    }

    public void Dispose()
    {
        _stateSubscription?.Dispose();
    }
}