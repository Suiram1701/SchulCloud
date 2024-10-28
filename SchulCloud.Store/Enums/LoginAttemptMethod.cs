﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Store.Enums;

/// <summary>
/// Different methods that can be used to log in into a user's account or verify 2fa with.
/// </summary>
public enum LoginAttemptMethod
{
    Password,
    Passkey,
    Authenticator,
    Email,
    SecurityKey,
    RecoveryCode
}
