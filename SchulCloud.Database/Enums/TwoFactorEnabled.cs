using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Database.Enums;

/// <summary>
/// Flags that indicates which 2fa methods are enabled.
/// </summary>
[Flags]
public enum TwoFactorEnabled
{
    /// <summary>
    /// General enabled and Authenticator app enabled.
    /// </summary>
    Authenticator = 1,

    /// <summary>
    /// Recovery codes are enabled.
    /// </summary>
    Recovery = 2
}
