using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SchulCloud.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Database.ValueConverters;

internal class ApiKeyPermissionConverter : ValueConverter<Dictionary<string, PermissionLevel>, string>
{
    public ApiKeyPermissionConverter() : base(
        convertToProviderExpression: model => ConvertToProvider(model),
        convertFromProviderExpression: provider => ConvertToModel(provider))
    {
    }

    private static new string ConvertToProvider(Dictionary<string, PermissionLevel> permission)
    {
        StringBuilder sb = new();
        foreach ((string permissionName, PermissionLevel level) in permission)
        {
            sb.Append(permissionName).Append(':').Append(level).Append(';');
        }

        return sb.ToString();
    }

    private static Dictionary<string, PermissionLevel> ConvertToModel(string provider)
    {
        Dictionary<string, PermissionLevel> permissions = [];
        foreach (string part in provider.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] kvp = part.Split(':', 2);
            permissions.Add(kvp[0], Enum.Parse<PermissionLevel>(kvp[1]));
        }

        return permissions;
    }
}
