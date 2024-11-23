using Mapster;
using System.Drawing;

namespace SchulCloud.RestApi.Models;

/// <summary>
/// A single role.
/// </summary>
public class Role
{
    /// <summary>
    /// The unique identifier of the role.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// The name of the name.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// The color of this role.
    /// </summary>
    public Color? Color { get; set; } = default!;

    internal static readonly TypeAdapterConfig AdapterConfig;

    static Role()
    {
        AdapterConfig = new();
        AdapterConfig.ForType<int?, Color?>().MapWith(color => color == null ? null : System.Drawing.Color.FromArgb(color.Value));
    }
}
