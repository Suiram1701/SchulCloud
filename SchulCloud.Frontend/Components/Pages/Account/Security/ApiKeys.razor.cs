﻿using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SchulCloud.Frontend.Options;
using SchulCloud.Identity.Models;
using SchulCloud.Identity.Options;

namespace SchulCloud.Frontend.Components.Pages.Account.Security;

[Route("/account/security/apiKeys")]
public sealed partial class ApiKeys : ComponentBase
{
    #region Injections
    [Inject]
    private IStringLocalizer<ApiKeys> Localizer { get; set; } = default!;

    [Inject]
    private IOptions<ApiKeyOptions> ApiKeyOptions { get; set; } = default!;

    [Inject]
    private IOptionsSnapshot<ApiOptions> ApiOptionsSnapshoot { get; set; } = default!;

    [Inject]
    private ApplicationUserManager UserManager { get; set; } = default!;
    #endregion

    private IEnumerable<UserApiKey> _apiKeys = [];

    [CascadingParameter]
    private Task<ApplicationUser> CurrentUser { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        ApplicationUser user = await CurrentUser;
        _apiKeys = await UserManager.GetApiKeysByUserAsync(user);
    }
}
