using Microsoft.AspNetCore.Identity;
using SchulCloud.Database.Enums;

namespace SchulCloud.Database.Models;

public class User : IdentityUser
{
    public override bool TwoFactorEnabled
    {
        get => false;
        set { }
    }

    /// <summary>
    /// Flags that indicates which 2fa methods are enabled.
    /// </summary>
    public TwoFactorMethod TwoFactorEnabledFlags { get; set; }
}