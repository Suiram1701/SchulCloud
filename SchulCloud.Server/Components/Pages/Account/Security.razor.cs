using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Server.Components.Modals;

namespace SchulCloud.Server.Components.Pages.Account;

[Authorize]
[Route("/account/security")]
public sealed partial class Security : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<Security> Localizer { get; set; } = default!;

    [Inject]
    private IOptions<PasswordOptions> PasswordOptionsAccessor { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IdentityErrorDescriber ErrorDescriber { get; set; } = default!;
    #endregion

    private PasswordChangeModal _passwordChangeModal = default!;

    private async Task PasswordChange_ClickAsync()
    {
        await _passwordChangeModal.Modal.ShowAsync().ConfigureAwait(false);
    }

    private void PasswordReset_Click()
    {
        string resetUrl = NavigationManager.GetUriWithQueryParameters("/auth/resetPassword", new Dictionary<string, object?>
        {
            ["returnUrl"] = NavigationManager.ToBaseRelativePath(NavigationManager.Uri)
        });
        NavigationManager.NavigateTo(resetUrl);
    }
}
