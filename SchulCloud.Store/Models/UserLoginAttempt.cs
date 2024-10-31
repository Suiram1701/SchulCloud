using SchulCloud.Store.Enums;
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
    /// The method that were used for the attempt.
    /// </summary>
    public LoginAttemptMethod Method { get; set; }

    /// <summary>
    /// The result of the attempt.
    /// </summary>
    public LoginAttemptResult? Result { get; set; }

    /// <summary>
    /// The ip address of the client attempted to login.
    /// </summary>
    public IPAddress IpAddress { get; set; } = default!;

    /// <summary>
    /// The located latitude of the client.
    /// </summary>
    /// <remarks>
    /// If <c>null</c> the client wasn't located on login.
    /// </remarks>
    ///
    public decimal? Latitude { get; set; }

    /// <summary>
    /// The located longitude of the client.
    /// </summary>
    /// <remarks>
    /// If <c>null</c> the client wasn't located on login.
    /// </remarks>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// The user agent used to login.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// The date time were the attempt occurred.
    /// </summary>
    public DateTime DateTime { get; set; }
}
