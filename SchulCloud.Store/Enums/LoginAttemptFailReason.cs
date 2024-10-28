using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Store.Enums;

/// <summary>
/// Different reasons why a log in attempt failed.
/// </summary>
public enum LoginAttemptFailReason
{
    /// <summary>
    /// The default reason. For example a wrong password.
    /// </summary>
    Default,

    /// <summary>
    /// A second factor a required to continue log in.
    /// </summary>
    TwoFactorRequired,

    /// <summary>
    /// The account is locked. Reasons could be that an admin locked the account or too many failed attempts.
    /// </summary>
    LockedOut,

    /// <summary>
    /// It is currently not allowed to log in into that account.
    /// </summary>
    NotAllowed
}
