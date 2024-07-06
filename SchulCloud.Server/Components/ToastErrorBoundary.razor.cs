using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using SchulCloud.Server.Extensions;
using System.Diagnostics;

namespace SchulCloud.Server.Components;

/// <summary>
/// An error boundary that captures exceptions log them and display the details in a toast to the user.
/// </summary>
/// <remarks>
/// Exception details are only displayed in development environment in every else environment are only trace and span id displayed.
/// </remarks>
public partial class ToastErrorBoundary : ErrorBoundaryBase
{
    #region Injections
    [Inject]
    private IHostEnvironment HostEnvironment { get; set; } = default!;

    [Inject]
    private ILogger<ToastErrorBoundary> Logger { get; set; } = default!;

    [Inject]
    private IStringLocalizer<ToastErrorBoundary> Localizer { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;
    #endregion

    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogWarning(exception, "An unhandled exception was captured down the render tree.");

        if (HostEnvironment.IsDevelopment())
        {
            ToastService.NotifyError(exception, Localizer["toastTitleEx"]);
        }
        else
        {
            Activity? activity = Activity.Current;
            string traceId = activity?.TraceId.ToString() ?? "N/A";
            string spanId = activity?.SpanId.ToString() ?? "N/A";
            ToastService.NotifyError(Localizer["toastTitle"], Localizer["toastProdMessage", traceId, spanId]);
        }

        return Task.CompletedTask;
    }
}
