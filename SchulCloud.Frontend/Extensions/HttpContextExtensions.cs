using System.Diagnostics.CodeAnalysis;

namespace SchulCloud.Frontend.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// Indicates whether a http context is valid.
    /// </summary>
    /// <param name="context">The context to verify.</param>
    /// <returns>The result.</returns>
    public static bool IsContextValid([NotNullWhen(true)] this HttpContext? context)
    {
        if (context is null)
        {
            return false;
        }

        return !context.WebSockets.IsWebSocketRequest;
    }
}
