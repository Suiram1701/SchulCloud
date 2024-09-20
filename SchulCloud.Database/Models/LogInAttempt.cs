namespace SchulCloud.Database.Models;

/// <summary>
/// Represents a login attempt for a user.
/// </summary>
public class LogInAttempt
{
    /// <summary>
    /// The id of this attempt.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The id of the user to attempted to log in.
    /// </summary>
    public string UserId { get; set; } = default!;

    /// <summary>
    /// A three letter code that for the used log in method.
    /// </summary>
    public string MethodCode { get; set; } = default!;

    /// <summary>
    /// Indicates whether the attempt succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// The ip address of the client attempted to log in.
    /// </summary>
    public byte[] IpAddress { get; set; } = [];

    /// <summary>
    /// The user agent used to log in.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// The date time were the attempt occurred.
    /// </summary>
    public DateTime DateTime { get; set; }
}
