using Microsoft.AspNetCore.Authorization;

namespace SchulCloud.Authorization.Attributes;

/// <summary>
/// An attribute that shows that this endpoint requires a permission with a minimum permission level.
/// </summary>
/// <param name="name">The name of the required permission.</param>
/// <param name="level">The minimum required level.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute(string name, PermissionLevel level) : AuthorizeAttribute($"{name}-{level}")
{
    /// <summary>
    /// The name of the permission.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// The minimum required level of the permission.
    /// </summary>
    public PermissionLevel Level => level;
}
