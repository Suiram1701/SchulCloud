using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SchulCloud.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.Database.ValueConverters;

internal class ApiKeyPermissionConverter : ValueConverter<HashSet<Permission>, string>
{
    public ApiKeyPermissionConverter() : base(
        convertToProviderExpression: model => string.Join(';', model),
        convertFromProviderExpression: provider => provider.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(Permission.Parse).ToHashSet())
    {
    }
}
