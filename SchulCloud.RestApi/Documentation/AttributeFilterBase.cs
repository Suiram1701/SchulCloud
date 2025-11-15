using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Microsoft.OpenApi;

namespace SchulCloud.RestApi.Documentation;

internal abstract class AttributeFilterBase<TAttribute> : IOperationFilter, IOperationAsyncFilter
    where TAttribute : Attribute
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        TAttribute[] attributes = [.. context.MethodInfo.GetCustomAttributes<TAttribute>()];
        if (attributes.Length != 0)
            Apply(operation, context, attributes);
    }

    public async Task ApplyAsync(OpenApiOperation operation, OperationFilterContext context, CancellationToken cancellationToken)
    {
        TAttribute[] attributes = [.. context.MethodInfo.GetCustomAttributes<TAttribute>()];
        if (attributes.Length != 0)
        {
            await ApplyAsync(operation, context, attributes, cancellationToken).ConfigureAwait(false);
        }
    }

    protected virtual void Apply(OpenApiOperation operation, OperationFilterContext context, TAttribute[] attributes)
    {
    }

    protected virtual Task ApplyAsync(OpenApiOperation operation, OperationFilterContext context, TAttribute[] attributes, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
