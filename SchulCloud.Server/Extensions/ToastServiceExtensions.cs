using BlazorBootstrap;
using Microsoft.AspNetCore.Identity;

namespace SchulCloud.Server.Extensions;

/// <summary>
/// Provides extensions for the <see cref="ToastService"/> with prepared toast.
/// </summary>
public static class ToastServiceExtensions
{
    /// <summary>
    /// Notifies of a success.
    /// </summary>
    /// <param name="service">The service to use.</param>
    /// <param name="title">The title of the toast.</param>
    /// <param name="message">The message of the toast.</param>
    public static void NotifySuccess(this ToastService service, string title, string message)
    {
        ToastMessage toast = new(ToastType.Success, IconName.CheckCircle, title, message)
        {
            AutoHide = true
        };
        service.Notify(toast);
    }

    /// <summary>
    /// Notifies of an error.
    /// </summary>
    /// <param name="service">The service to use.</param>
    /// <param name="title">The title of the toast.</param>
    /// <param name="message">The message of the toast.</param>
    public static void NotifyError(this ToastService service, string title, string message)
    {
        ToastMessage toast = new(ToastType.Danger, IconName.XCircle, title, message);
        service.Notify(toast);
    }

    /// <summary>
    /// Notifies of an error.
    /// </summary>
    /// <param name="service">The service to use.</param>
    /// <param name="ex">The exception to display.</param>
    /// <param name="title">The title of the toast.</param>
    /// <param name="message">An optional message to display at the beginning.</param>
    public static void NotifyError(this ToastService service, Exception ex, string title, string? message = null)
    {
        message ??= string.Empty;
        if (!message.EndsWith(' '))
        {
            message += ' ';
        }
        message += ex.ToString();

        ToastMessage toast = new(ToastType.Danger, IconName.XCircle, title, message);
        service.Notify(toast);
    }

    /// <summary>
    /// Notifies of an error.
    /// </summary>
    /// <param name="service">The service to use.</param>
    /// <param name="errors">The errors to display.</param>
    /// <param name="title">The title of the toast.</param>
    /// <param name="message">An optional message to display at the beginning.</param>
    public static void NotifyError(this ToastService service, IEnumerable<IdentityError> errors, string title, string? message = null)
    {
        message ??= string.Empty;
        if (!message.EndsWith(' '))
        {
            message += ' ';
        }
        message += string.Join(' ', errors.Select(err => err.Description.Trim()));

        ToastMessage toast = new(ToastType.Danger, IconName.XCircle, title, message);
        service.Notify(toast);
    }
}
