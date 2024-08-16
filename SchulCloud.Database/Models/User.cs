using Microsoft.AspNetCore.Identity;
using SchulCloud.Database.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchulCloud.Database.Models;

public class User : IdentityUser
{
    public override bool TwoFactorEnabled
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    /// <summary>
    /// Flags that indicates which 2fa methods are enabled.
    /// </summary>
    internal TwoFactorMethod TwoFactorEnabledFlags { get; set; }
}