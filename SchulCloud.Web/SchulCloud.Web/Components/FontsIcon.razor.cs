using Microsoft.AspNetCore.Components;

namespace SchulCloud.Web.Components;

/// <summary>
/// A Google Fonts Icon symbol.
/// </summary>
public partial class FontsIcon : ComponentBase
{
    /// <summary>
    /// The name of the icon
    /// </summary>
    [Parameter]
    public required string Name { get; init; }
}
