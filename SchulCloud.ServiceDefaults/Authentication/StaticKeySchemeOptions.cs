using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.ServiceDefaults.Authentication;

internal class StaticKeySchemeOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// The static key to use.
    /// </summary>
    public string? Key { get; set; }
}
