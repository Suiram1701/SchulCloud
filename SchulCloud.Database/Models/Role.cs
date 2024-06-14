using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Database.Models;

public class Role : IdentityRole
{
    /// <summary>
    /// Indicates whether this is a default role that can't get deleted and has a minimum of permissions.
    /// </summary>
    public bool DefaultRole { get; set; } = false;

    /// <summary>
    /// The hex color of this role.
    /// </summary>
    [StringLength(9)]
    public string? Color { get; set; }
}
