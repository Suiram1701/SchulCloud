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

    /// <summary>
    /// Performs a save navigation by ensure that the <paramref name="uri"/> is only used as an relative.
    /// </summary>
    /// <param name="manager">The manager to use.</param>
    /// <param name="uri">The target uri.</param>
    /// <param name="forceLoad">Indicates whether the page should be loaded via HTTP instead of enhanced navigation.</param>
    /// <param name="replace">Indicates whether the addresss uri should be replaced with the new one.</param>
    public static void NavigateSaveTo(this NavigationManager manager, string uri, bool forceLoad = false, bool replace = false)
    {
        ArgumentNullException.ThrowIfNull(manager);

        Uri targetUri = manager.ToAbsoluteUri(uri);
        manager.NavigateTo(targetUri.PathAndQuery, forceLoad, replace);
    }
}
