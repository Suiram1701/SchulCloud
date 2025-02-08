using GoogleMapsComponents.Maps;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MyCSharp.HttpUserAgentParser;
using MyCSharp.HttpUserAgentParser.Providers;
using Microsoft.JSInterop;
using GoogleMapsComponents;
using SchulCloud.Identity.Models;

namespace SchulCloud.Frontend.Components.Pages.Account.Security;

[Route("/account/security/loginAttempts/{attemptId}")]
public sealed partial class LoginAttemptDetails : ComponentBase, IAsyncDisposable
{
    #region Injections
    [Inject]
    private IStringLocalizer<LoginAttemptDetails> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private IHttpUserAgentParserProvider UserAgentParserProvider { get; set; } = default!;

    [Inject]
    private ApplicationUserManager UserManager { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    #endregion

    private bool _loaded;

    private ApplicationUser _user = default!;

    private bool _authorizedAccess;
    private UserLoginAttempt? _attempt;
    private HttpUserAgentInformation? _userAgent;

    private GoogleMap? _map;
    private Marker? _mapMarker;
    private MapOptions? _mapOptions;
    private IJSObjectReference? _importRef;

    [Parameter]
    public string AttemptId { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "originPage")]
    public int? OriginPage { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _attempt = await UserManager.FindLoginAttemptAsync(AttemptId);
            if (_attempt is not null)
            {
                AuthenticationState authenticationState = await AuthenticationState;
                _user = (await UserManager.FindUserByLoginAttemptAsync(_attempt))!;
                _authorizedAccess = UserManager.GetUserId(authenticationState.User) == await UserManager.GetUserIdAsync(_user);

                if (_authorizedAccess)
                {
                    if (!string.IsNullOrWhiteSpace(_attempt.UserAgent))
                    {
                        _userAgent = UserAgentParserProvider.Parse(_attempt.UserAgent!);
                    }

                    if (_attempt.Latitude is not null && _attempt.Longitude is not null)
                    {
                        await InitializeMapAsync();
                    }
                }
            }

            _loaded = true;
            StateHasChanged();
        }
    }

    private async Task InitializeMapAsync()
    {
        (bool success, _importRef) = await JSRuntime.InvokeAsyncWithErrorHandling<IJSObjectReference>("import", "/_content/BlazorGoogleMaps/js/objectManager.js");
        if (success)
        {
            _mapOptions = new()
            {
                MapTypeId = MapTypeId.Satellite,
                Center = new(_attempt!.Latitude!.Value, _attempt.Longitude!.Value),
                Zoom = 10,
            };
        }
    }
    
    private async Task Map_OnAfterInitAsync()
    {
        _mapMarker = await Marker.CreateAsync(_map!.JsRuntime, new()
        {
            Map = _map.InteropObject,
            Position = new(_attempt!.Latitude!.Value, _attempt.Longitude!.Value),
            Label = new MarkerLabel()
            {
                Text = _attempt.IpAddress.ToString(),
                FontSize = "20px",
                Color = Colors.Red.Default,
                ClassName = "mb-13"
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_mapMarker is not null)
            {
                await _mapMarker.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }

        try
        {
            if (_map is not null)
            {
                await _map.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }

        try
        {
            if (_importRef is not null)
            {
                await _importRef.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
    }
}