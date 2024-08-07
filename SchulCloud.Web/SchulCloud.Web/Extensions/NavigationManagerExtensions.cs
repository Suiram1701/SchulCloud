using Microsoft.AspNetCore.Components;

namespace SchulCloud.Web.Extensions;

public static class NavigationManagerExtensions
{
    /// <summary>
    /// Gets the current relative uri.
    /// </summary>
    /// <param name="manager">The nav manager to use.</param>
    /// <returns>The relative uri.</returns>
    public static string GetRelativeUri(this NavigationManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager, nameof(manager));
        return manager.ToBaseRelativePath(manager.Uri);
    }
}
