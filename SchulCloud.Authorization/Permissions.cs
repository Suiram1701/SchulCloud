using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Authorization;

/// <summary>
/// A class containing strings representing different permissions.
/// </summary>
public static class Permissions
{
    /// <summary>
    /// The permission to add, edit or remove users.
    /// </summary>
    public const string Users = "users";

    /// <summary>
    /// The permission to add, edit or remove roles.
    /// </summary>
    public const string Roles = "roles";
}
