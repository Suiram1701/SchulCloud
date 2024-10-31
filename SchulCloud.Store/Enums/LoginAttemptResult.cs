using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Store.Enums;

/// <summary>
/// The result of a login attempt.
/// </summary>
public enum LoginAttemptResult
{
    /// <summary>
    /// Indicates that the attempt succeeded.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The attempt failed. For example by a wrong password.
    /// </summary>
    Failed,

    /// <summary>
    /// A second factor was required to continue log in.
    /// </summary>
    TwoFactorRequired,

    /// <summary>
    /// The account was locked. Reasons could be that an admin locked the account or too many failed attempts.
    /// </summary>
    LockedOut,

    /// <summary>
    /// It was not allowed at the time to log in into that account.
    /// </summary>
    NotAllowed
}
