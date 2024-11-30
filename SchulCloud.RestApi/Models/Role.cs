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
    /// The 32 bit ARGB color of this role.
    /// </summary>
    public int? ArgbColor { get; set; } = default!;
}
