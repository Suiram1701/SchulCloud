using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using SchulCloud.Web.Extensions;
using SchulCloud.Web.Options;
using System.Text;

namespace SchulCloud.Web.Components.Pages.Account.Security.TwoFactor;

[Route("/account/security/twoFactor/recoveryCodes")]
public sealed partial class RecoveryCodes : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<RecoveryCodes> Localizer { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private IOptions<PresentationOptions> PresentationOptionsAccessor { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    #endregion

    private ApplicationUser _user = default!;

    private IEnumerable<string>? _recoveryCodes;
    private const int _codeCount = 10;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState;
        _user = (await UserManager.GetUserAsync(authenticationState.User))!;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Send the codes only via SignalR to prevent multiple transmissions.
            _recoveryCodes = await CreateRecoveryCodesAsync();
            StateHasChanged();
        }
    }

    private async Task Download_ClickAsync()
    {
        if (_recoveryCodes?.Any() ?? false)
        {
            string appName = PresentationOptionsAccessor.Value.ApplicationName;
            string userName = (await UserManager.GetUserNameAsync(_user))!;
            string userEmail = (await UserManager.GetEmailAsync(_user))!;

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

    private async Task<IEnumerable<string>?> CreateRecoveryCodesAsync()
    {
        IEnumerable<string>? recoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(_user, _codeCount);
        if (recoveryCodes is null)
        {
            SnackbarService.AddError(Localizer["generationError"]);
        }

        return recoveryCodes;
    }
}