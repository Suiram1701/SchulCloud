using Microsoft.EntityFrameworkCore.ChangeTracking;
using SchulCloud.Authorization;

namespace SchulCloud.Database.ValueComparers;

internal class ApiKeyPermissionComparer : ValueComparer<HashSet<Permission>>
{
    public ApiKeyPermissionComparer() : base(
        equalsExpression: (obj1, obj2) => ValueEquals(obj1, obj2),
        hashCodeExpression: obj => string.Join('_', obj).GetHashCode())
    {
    }

    private static bool ValueEquals(HashSet<Permission>? obj1, HashSet<Permission>? obj2)
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
            return obj1.SequenceEqual(obj2);
        }
    }
}
