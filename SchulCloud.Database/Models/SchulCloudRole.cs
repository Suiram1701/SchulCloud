using Microsoft.AspNetCore.Identity;

namespace SchulCloud.Database.Models;

public class SchulCloudRole : IdentityRole
{
    /// <summary>
    /// Indicates whether this is a default role that can't get deleted and has a minimum of permissions.
    /// </summary>
    public bool DefaultRole { get; set; } = false;

    /// <summary>
    /// The hex color of this role.
    /// </summary>
    public int? ArgbColor { get; set; }
}
