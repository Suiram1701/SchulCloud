using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using SchulCloud.Frontend.Extensions;
using System.Diagnostics;

namespace SchulCloud.Frontend.Components.Routing;

/// <summary>
/// An error boundary that captures exceptions log them and display the details in a toast to the user.
/// </summary>
/// <remarks>
/// Exception details are only displayed in development environment in every else environment are only trace and span id displayed.
/// </remarks>
public partial class GlobalErrorBoundary : ErrorBoundaryBase
{
    #region Injections
    [Inject]
    private IHostEnvironment HostEnvironment { get; set; } = default!;

    [Inject]
    private ILogger<GlobalErrorBoundary> Logger { get; set; } = default!;

    [Inject]
    private IStringLocalizer<GlobalErrorBoundary> Localizer { get; set; } = default!;

    [Inject]
    private ISnackbar SnackbarService { get; set; } = default!;
    #endregion

    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogWarning(exception, "An unhandled exception was captured down the render tree.");

        if (HostEnvironment.IsDevelopment())
        {
            SnackbarService.AddError(exception, Localizer["toastEx"]);
        }
        else
        {
            Activity? activity = Activity.Current;
            string traceId = activity?.TraceId.ToString() ?? "N/A";
            string spanId = activity?.SpanId.ToString() ?? "N/A";
            SnackbarService.AddError(Localizer["toastProd", traceId, spanId]);
        }

        return Task.CompletedTask;
    }
}
