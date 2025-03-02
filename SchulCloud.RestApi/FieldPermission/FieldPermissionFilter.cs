using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SchulCloud.Authorization.Extensions;
using SchulCloud.RestApi.Linq;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Reflection;

namespace SchulCloud.RestApi.FieldPermission;

/// <summary>
/// A result filter that removes fields if the requesting user isn't authorized to access those.
/// </summary>
public class FieldPermissionFilter : IAsyncResultFilter, IOrderedFilter
{
    /// <inheritdoc />
    public int Order => 7;

    /// <inheritdoc />
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<FieldPermissionFilter>>();

        if (context.Result is ObjectResult result && IsSuccessCode(result.StatusCode))
        {
            Type? valueType = result.Value?.GetType();
            if (valueType is null)
            {
                await next().ConfigureAwait(false);
                return;
            }

            Type? itemType = valueType.GenericTypeArguments.Length == 1
                ? valueType.GenericTypeArguments[0]
                : null;
            if (itemType is not null && valueType.IsAssignableTo(typeof(IEnumerable<>).MakeGenericType(itemType)))
            {
                PropertyInfo[] removeProperties = [.. await GetRemovalPropertiesAsync(itemType, context).ConfigureAwait(false)];
                if (removeProperties.Length == 0)
                {
                    await next().ConfigureAwait(false);
                    return;
                }

                if (valueType.IsAssignableTo(typeof(IQueryable<>).MakeGenericType(itemType)))
                {
                    // Exclude using IQueryable
                    IQueryable query = (IQueryable)result.Value!;

                    UpdateSelectVisitor selectVisitor = new((_, exp) => RemovePropsFromSelectorInit(exp, removeProperties));
                    Expression newQueryExpression = selectVisitor.Visit(query.Expression)!;
                    if (selectVisitor.Visited)
                    {
                        // Already a select call placed (mostly by projection using Mapster), so update the existing one using a expression visitor
                        result.Value = query.Provider.CreateQuery(newQueryExpression);
                    }
                    else
                    {
                        // No select call placed so place a new
                        Type sourceType = Type.MakeGenericMethodParameter(0);
                        Type resultType = Type.MakeGenericMethodParameter(1);
                        Type[] parameterTypes = [
                            typeof(IQueryable<>).MakeGenericType(sourceType),
                            typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(sourceType, resultType))];

                        MethodInfo selectMethod = typeof(Queryable)
                            .GetMethod(nameof(Queryable.Select), BindingFlags.Public | BindingFlags.Static, parameterTypes)!
                            .MakeGenericMethod(itemType, itemType);

                        LambdaExpression LambdaExpression = BuildRemovalLambda(itemType, removeProperties);
                        result.Value = selectMethod.Invoke(null, parameters: [result.Value, LambdaExpression]);
                    }

                    LogPropertiesRemoved(logger, removeProperties, "item query");
                }
                else
                {
                    // Exclude using IEnumerable
                    Type sourceType = Type.MakeGenericMethodParameter(0);
                    Type resultType = Type.MakeGenericMethodParameter(1);
                    Type[] parameterTypes = [typeof(IEnumerable<>).MakeGenericType(sourceType), typeof(Func<,>).MakeGenericType(sourceType, resultType)];

                    MethodInfo selectMethod = typeof(Enumerable)
                        .GetMethod(nameof(Enumerable.Select), BindingFlags.Public | BindingFlags.Static, parameterTypes)!
                        .MakeGenericMethod(itemType, itemType);

                    LambdaExpression LambdaExpression = BuildRemovalLambda(itemType, removeProperties);
                    result.Value = selectMethod.Invoke(null, parameters: [result.Value, LambdaExpression.Compile()]);

                    LogPropertiesRemoved(logger, removeProperties, "item collection");
                }
            }
            else
            {
                // Exclude properties from a single item
                List<PropertyInfo> removedProps = [];
                foreach (PropertyInfo property in await GetRemovalPropertiesAsync(valueType, context).ConfigureAwait(false))
                {
                    if (!property.CanWrite)
                    {
                        logger.LogError("A setter were expected at property '{property}' of type '{type}'.", property.Name, property.DeclaringType);
                        throw new InvalidOperationException($"Property '{property.Name}' of type '{property.DeclaringType}' have to has a setter.");
                    }

                    property.SetValue(result.Value, null);
                    removedProps.Add(property);
                }

                if (removedProps.Count != 0)
                {
                    LogPropertiesRemoved(logger, removedProps, "item");
                }
            }
        }

        await next().ConfigureAwait(false);
    }

    private static async Task<IEnumerable<PropertyInfo>> GetRemovalPropertiesAsync(Type declaringType, ResultExecutingContext context)
    {
        IAuthorizationService authorization = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();

        List<PropertyInfo> properties = [];
        foreach (PropertyInfo property in declaringType.GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [])
        {
            IEnumerable<Task<AuthorizationResult>> permissionResults = property
                .GetCustomAttributes<RequireFieldPermissionAttribute>()
                .Select(attribute => authorization.RequirePermissionAsync(context.HttpContext.User, attribute.Name, attribute.Level));
            AuthorizationResult[] results = await Task.WhenAll(permissionResults).ConfigureAwait(false);

            if (!results.All(result => result.Succeeded))
                properties.Add(property);
        }

        return properties;
    }

    private static MemberInitExpression RemovePropsFromSelectorInit(Expression orgExpression, PropertyInfo[] removalProps)
    {
        MemberInitExpression initExpression = orgExpression as MemberInitExpression
            ?? throw new InvalidOperationException($"A {nameof(MemberInitExpression)} were expected.");

        MemberBinding?[] bindings = [.. initExpression.Bindings];
        for (int i = 0; i < bindings.Length; i++)
        {
            MemberInfo member = bindings[i]!.Member;     // Can only be assigned later to null
            PropertyInfo property = member.DeclaringType!.GetProperty(member.Name)!;

            if (removalProps.Contains(property))
            {
                if (property.GetCustomAttribute<RequiredMemberAttribute>() is null)     // If not required a null assignment is not needed
                {
                    bindings[i] = null;
                }
                else
                {
                    ConstantExpression newValue = Expression.Constant(null, property.PropertyType);
                    bindings[i] = Expression.Bind(member, newValue);
                }
            }
        }

        return initExpression.Update(initExpression.NewExpression, bindings.Where(bind => bind is not null)!);
    }

    private static LambdaExpression BuildRemovalLambda(Type declaringType, PropertyInfo[] removeProperties)
    {
        PropertyInfo[] typeProperties = declaringType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        ParameterExpression itemParamExpression = Expression.Parameter(declaringType, "o");

        List<MemberBinding> initializeMembers = [];
        foreach (PropertyInfo property in typeProperties)
        {
            Expression valueExpression = !removeProperties.Contains(property)
                ? Expression.PropertyOrField(itemParamExpression, property.Name)
                : Expression.Constant(null, property.PropertyType);
            initializeMembers.Add(Expression.Bind(property, valueExpression));
        }

        MemberInitExpression initExpression = Expression.MemberInit(Expression.New(declaringType), initializeMembers);
        return Expression.Lambda(initExpression, itemParamExpression);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254", Justification = "The kind of the removal shouldn't be a parameter.")]
    private static void LogPropertiesRemoved(ILogger logger, IEnumerable<PropertyInfo> props, string kind)
    {
        string formattedProperties = string.Join(", ", props.Select(prop => $"'{prop.Name}'"));
        logger.LogInformation($"Sensitive properties {{props}} removed from {kind}.", formattedProperties);
    }

    private static bool IsSuccessCode(int? code) => code is >= 200 and < 300;
}
