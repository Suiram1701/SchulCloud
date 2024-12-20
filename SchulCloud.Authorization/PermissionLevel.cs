namespace SchulCloud.Authorization;

/// <summary>
/// Different levels of permissions.
/// </summary>
/// <remarks>
/// Having a specific permission level means that every lower level is included.
/// </remarks>
public enum PermissionLevel
{
    /// <summary>
    /// No permission.
    /// </summary>
    None,

    /// <summary>
    /// Read permission.
    /// </summary>
    Read,

    /// <summary>
    /// Write permission.
    /// </summary>
    Write,

    /// <summary>
    /// Permission for special operations.
    /// </summary>
    Special
}
