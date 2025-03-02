using SchulCloud.Authorization;

namespace SchulCloud.RestApi.FieldPermission;

/// <summary>
/// Marks a field or property that it's requires a specific permission to be returned.
/// </summary>
/// <param name="name">The required permission name.</param>
/// <param name="level">The least required permission level.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public class RequireFieldPermissionAttribute(string name, PermissionLevel level) : Attribute
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
