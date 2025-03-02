using SchulCloud.Authorization;

namespace SchulCloud.RestApi.Models;

/* 
 * This class like record declaration is required for EF Core to work properly with type projection.
 * -> EF Core can't work well with constructors.
*/

/// <summary>
/// A single user.
/// </summary>
public record User
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The unique name of the user.
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// The unique email of the user.
    /// </summary>
    [RequireFieldPermission(Permissions.Users, PermissionLevel.Read)]
    public string? Email { get; init; }

    /// <summary>
    /// The phone number of the user.
    /// </summary>
    [RequireFieldPermission(Permissions.Users, PermissionLevel.Read)]
    public string? PhoneNumber { get; init; }
}