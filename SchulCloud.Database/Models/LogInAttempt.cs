namespace SchulCloud.Database.Models;

/// <summary>
/// Represents a login attempt for a user.
/// </summary>
internal class LoginAttempt
{
    /// <summary>
    /// The id of this attempt.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The id of the user to attempted to login.
    /// </summary>
    public string UserId { get; set; } = default!;

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
    public byte[] IpAddress { get; set; } = [];

    /// <summary>
    /// The located latitude of the client.
    /// </summary>
    /// <remarks>
    /// If <c>null</c> the client wasn't located on login.
    /// </remarks>
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
