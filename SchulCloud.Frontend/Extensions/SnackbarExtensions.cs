using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using MudBlazor;
using System.Text;

namespace SchulCloud.Frontend.Extensions;

public static class SnackbarExtensions
{
    public static Snackbar? AddSuccess(this ISnackbar service, string message, string? key = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Add(message, Severity.Success, key: key);
    }

    public static Snackbar? AddInfo(this ISnackbar service, string message, string? key = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Add(message, Severity.Info, key: key);
    }

    public static Snackbar? AddError(this ISnackbar service, string message, string? key = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Add(message, Severity.Error, ConfigureErrorOption, key: key);
    }

    public static Snackbar? AddError(this ISnackbar service, IEnumerable<IdentityError> errors, string? message, string? key = null)
    {
        ArgumentNullException.ThrowIfNull(service);

        StringBuilder sb = new();
        sb.AppendFormat("{0}<br />", message);
        sb.Append("<ul class=\"mt-2\">");
        foreach (IdentityError error in errors)
        {
            sb.AppendFormat("<li>{0}</li>", error.Description);
        }
        sb.Append("</ul>");

        return service.Add(new MarkupString(sb.ToString()), Severity.Error, ConfigureErrorOption, key: key);
    }

    public static Snackbar? AddError(this ISnackbar service, Exception exception, string message, string? key = null)
    {
        ArgumentNullException.ThrowIfNull(service);

        StringBuilder sb = new();
        sb.AppendFormat("{0}<br />", message);
        sb.Append(exception.ToString());

        return service.Add(new MarkupString(sb.ToString()), Severity.Error, ConfigureErrorOption, key: key);
    }

    private static void ConfigureErrorOption(SnackbarOptions option)
    {
        option.RequireInteraction = true;
    }
}
