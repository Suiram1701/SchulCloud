using System.Net;

namespace SchulCloud.Store.Models;

/// <summary>
/// Represents a login attempt for a user.
/// </summary>
public class UserLoginAttempt
{
    /// <summary>
    /// The id of this attempt.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// A code that represents the used login method.
    /// </summary>
    public string Method { get; set; } = default!;

    /// <summary>
    /// Indicates whether the attempt succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// The ip address of the client attempted to login.
    /// </summary>
    public IPAddress IpAddress { get; set; } = default!;

    /// <summary>
    /// The user agent used to login.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// The date time were the attempt occurred.
    /// </summary>
    public DateTime DateTime { get; set; }
}
