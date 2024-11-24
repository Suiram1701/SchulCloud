using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.ServiceDefaults.Options;

/// <summary>
/// Options for a service.
/// </summary>
public class ServiceOptions
{
    /// <summary>
    /// The path prefix of the application
    /// </summary>
    public string BasePath { get; set; } = default!;
}
