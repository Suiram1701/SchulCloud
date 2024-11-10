using Microsoft.EntityFrameworkCore.ChangeTracking;
using SchulCloud.Authorization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Database.ValueComparers;

internal class ApiKeyPermissionComparer : ValueComparer<Dictionary<string, PermissionLevel>>
{
    public ApiKeyPermissionComparer() : base(
        equalsExpression: (obj1, obj2) => ValueEquals(obj1, obj2),
        hashCodeExpression: obj => string.Concat(obj.Select(kvp => $"{kvp.Key}:{kvp.Value};")).GetHashCode())
    {
    }

    private static bool ValueEquals(Dictionary<string, PermissionLevel>? obj1, Dictionary<string, PermissionLevel>? obj2)
    {
        if (obj1 is null && obj2 is null)
        {
            return true;
        }
        else if (obj1 is null || obj2 is null)
        {
            return false;
        }
        else
        {
            return obj1.SequenceEqual(obj2, comparer: new PermissionsComparer());
        }
    }

    private class PermissionsComparer : IEqualityComparer<KeyValuePair<string, PermissionLevel>>
    {
        public bool Equals(KeyValuePair<string, PermissionLevel> x, KeyValuePair<string, PermissionLevel> y)
        {
            return x.Key.Equals(y.Key) && x.Value.Equals(y.Value);
        }

        public int GetHashCode([DisallowNull] KeyValuePair<string, PermissionLevel> obj)
        {
            return $"{obj.Key}:{obj.Value}".GetHashCode();
        }
    }
}
