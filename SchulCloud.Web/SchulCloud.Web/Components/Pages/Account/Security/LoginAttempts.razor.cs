using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MyCSharp.HttpUserAgentParser;
using MyCSharp.HttpUserAgentParser.Providers;
using SchulCloud.Store.Models;
using SchulCloud.Web.Components.Dialogs;
using SchulCloud.Web.Extensions;
using System.Net;

namespace SchulCloud.Web.Components.Pages.Account.Security;

[Route("/account/security/loginAttempts")]
public sealed partial class LoginAttempts : ComponentBase
{
    #region
    [Inject]
    private IStringLocalizer<LoginAttempts> Localizer { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private IHttpUserAgentParserProvider UserAgentParserProvider { get; set; } = default!;

    [Inject]
    private AppUserManager UserManager { get; set; } = default!;
    #endregion

    private ApplicationUser _user = default!;

    private IEnumerable<UserLoginAttempt> _attempts = [];
    private readonly Dictionary<string, HttpUserAgentInformation> _userAgents = [];

    private MudTable<UserLoginAttempt> _attemptTable = default!;

    [SupplyParameterFromQuery(Name = "page")]
    public int? Page { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationState;
        _user = (await UserManager.GetUserAsync(authenticationState.User))!;

        if (HttpContext is null)
        {
            _attempts = await UserManager.FindLoginAttemptsByUserAsync(_user);
            foreach (UserLoginAttempt attempt in _attempts)
            {
                if (string.IsNullOrWhiteSpace(attempt.UserAgent))
                {
                    continue;
                }

                HttpUserAgentInformation userAgent = UserAgentParserProvider.Parse(attempt.UserAgent);
                _userAgents.Add(attempt.Id, userAgent);
            }
        }
    }

    private async Task RemoveAllAttempts_ClickAsync()
    {
        IDialogReference dialogReference = await DialogService.ShowConfirmDialogAsync(
            Localizer["removeAllDialog"], 
            Localizer["removeAllDialog_Message"],
            confirmColor: Color.Error);
        if (await dialogReference.GetReturnValueAsync<bool?>() ?? false)
        {
            IdentityResult removeResult = await UserManager.RemoveAllLoginAttemptsOfUserAsync(_user);
            if (removeResult.Succeeded)
            {
                SnackbarService.AddSuccess(Localizer["removeAllSuccess"]);
                _attempts = [];
            }
            else
            {
                SnackbarService.AddError(removeResult.Errors, Localizer["removeAllError"]);
            }
        }
    }
}
