using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Authorization;

/// <summary>
/// A permission with a assigned permission level.
/// </summary>
/// <param name="Name">The name of the permission.</param>
/// <param name="Level">The level of the permission</param>
public record Permission(string Name, PermissionLevel Level) : IParsable<Permission>
{
    public override string ToString() => $"{Name}={Level}";

    /// <inheritdoc cref="Parse(string, IFormatProvider?)"/>
    public static Permission Parse(string s) => Parse(s, null);

    public static Permission Parse(string s, IFormatProvider? provider)
    {
        string[] parts = s.Split('=');
        if (parts.Length != 2)
        {
            throw new FormatException("Invalid format. Expected format: 'Name=Level'.");
        }

        string name = parts[0];
        if (!Enum.TryParse(parts[1], out PermissionLevel level))
        {
            throw new FormatException($"Invalid permission level '{parts[1]}'.");
        }

        return new(name, level);
    }

    /// <inheritdoc cref="TryParse(string?, IFormatProvider?, out Permission)"/>
    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out Permission result) => TryParse(s, out result);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Permission result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        string[] parts = s.Split('=');
        if (parts.Length != 2)
        {
            return false;
        }

        string name = parts[0];
        if (!Enum.TryParse(parts[1], out PermissionLevel level))
        {
            return false;
        }

        result = new(name, level);
        return true;
    }
}
