namespace SchulCloud.RestApi.Models;

/// <summary>
/// A single user.
/// </summary>
public class User
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// The unique name of the user.
    /// </summary>
    public string UserName { get; set; } = default!;

    /// <summary>
    /// The unique email of the user. This field will only be returned if the request was made with the permission **Users** at the level **Read** or greater.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The phone number of the user. This field will only be returned if the request was made with the permission **Users** at the level **Read** or greater.
    /// </summary>
    public string? PhoneNumber { get; set; }
}
