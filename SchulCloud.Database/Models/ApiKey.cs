using SchulCloud.Authorization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Database.Models;

/// <summary>
/// An api key for a user.
/// </summary>
internal class ApiKey
{
    /// <summary>
    /// The id of this api key.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The id of the user that owns this key.
    /// </summary>
    public string UserId { get; set; } = default!;

    /// <summary>
    /// The by the user specified name of the key.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Notes that are taken by the user for this key.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// The hash of the actual api key.
    /// </summary>
    public string KeyHash { get; set; } = default!;

    /// <summary>
    /// Indicates whether the key always have the same permissions than the user.
    /// </summary>
    public bool AllPermissions { get; set; }

    /// <summary>
    /// The permissions of the api key.
    /// </summary>
    public HashSet<Permission> PermissionLevels { get; set; } = [];

    /// <summary>
    /// The date time where the key was created.
    /// </summary>
    public DateTime Created { get; set; } = default!;

    /// <summary>
    /// The date time where the key will be expire. If <c>null</c> the key doesn't expire.
    /// </summary>
    public DateTime? Expiration { get; set; }
}
