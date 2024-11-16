﻿using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Authorization.Requirements;

/// <summary>
/// An requirement that requires the user to have a specific minimum level of a permission.
/// </summary>
/// <param name="name">The name of the required permission.</param>
/// <param name="level">The minimum level of the permission.</param>
public class PermissionRequirement(string name, PermissionLevel level) : IAuthorizationRequirement
{
    /// <summary>
    /// The name of the required permission.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// The required level of the permission.
    /// </summary>
    public PermissionLevel PermissionLevel { get; set; } = level;
}