using Microsoft.AspNetCore.Identity;
using SchulCloud.Database.Enums;

namespace SchulCloud.Database.Models;

public class User : IdentityUser
{
    /// <summary>
    /// Flags that indicates which 2fa methods are enabled.
    /// </summary>
    public new TwoFactorEnabled TwoFactorEnabled { get; set; }
}