using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SchulCloud.Authorization;

namespace SchulCloud.Database.ValueConverters;

internal class ApiKeyPermissionConverter : ValueConverter<HashSet<Permission>, string>
{
    public ApiKeyPermissionConverter() : base(
        convertToProviderExpression: model => string.Join(';', model),
        convertFromProviderExpression: provider => provider.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(Permission.Parse).ToHashSet())
    {
    }
}
