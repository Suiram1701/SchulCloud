using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Authorization;

/// <summary>
/// The claims types that are used by this service.
/// </summary>
public static class ClaimTypes
{
    /// <summary>
    /// A claim type that contains a permission name and level.
    /// </summary>
    public const string Permission = nameof(Permission);
}
