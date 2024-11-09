using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Authorization.Attributes;

/// <summary>
/// An attribute that shows that this endpoint requires a permission with a minimum permission level.
/// </summary>
/// <param name="name">The name of the required permission.</param>
/// <param name="level">The minimum required level.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute(string name, PermissionLevel level) : AuthorizeAttribute($"{name}-{level}")
{
}
