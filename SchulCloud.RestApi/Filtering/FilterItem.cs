using System.Linq.Expressions;
using System.Reflection;

namespace SchulCloud.RestApi.Filtering;

/// <summary>
/// A field to compare against a constant value.
/// </summary>
/// <typeparam name="TItem">The parent type of the field.</typeparam>
/// <param name="Property">The field to compare against the constant value.</param>
/// <param name="Operator">The operator to use for the comparison.</param>
/// <param name="Value">The constant value to compare against </param>
public record FilterItem<TItem>(PropertyInfo Property, FilterOperators Operator, object? Value)
    where TItem : class
{
    private static readonly MethodInfo _buildMethod = default!;

    static FilterItem()
    {
        _buildMethod = typeof(FilterItem<TItem>).GetMethod(
            name: nameof(BuildExpression),
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
            types: [typeof(ParameterExpression)])!;
    }

    /// <inheritdoc/>
    public BinaryExpression BuildExpression(ParameterExpression parameterExpression)
    {
        MethodInfo buildMethod = _buildMethod.MakeGenericMethod(Property.PropertyType);
        return (BinaryExpression)buildMethod.Invoke(this, parameters: [parameterExpression])!;
    }

    private BinaryExpression BuildExpression<TProperty>(ParameterExpression parameterExpression)
    {
        MemberExpression rowPropertyAccess = Expression.Property(parameterExpression, Property.Name);
        MemberExpression valuePropertyAccess = rowPropertyAccess;

        Type propertyType = Property.PropertyType;
        if (Property.PropertyType.GenericTypeArguments.Length > 0)
        {
            Type genericParam = Property.PropertyType.GenericTypeArguments[0];
            if (Property.PropertyType == typeof(Nullable<>).MakeGenericType(genericParam))
            {
                propertyType = genericParam;
                valuePropertyAccess = Expression.Property(rowPropertyAccess, nameof(Nullable<int>.Value));
            }
        }

        ConstantExpression valueExpression = Expression.Constant(Value, propertyType);

        switch (Operator)
        {
            case FilterOperators.Eq:
                return Value is null     // When comparing to null the nullable instance have to be used instead of the value accessed one.
                    ? Expression.Equal(rowPropertyAccess, valueExpression)
                    : Expression.Equal(valuePropertyAccess, valueExpression);
            case FilterOperators.Ne:
                return Value is null    // When comparing to null the nullable instance have to be used instead of the value accessed one.
                    ? Expression.NotEqual(rowPropertyAccess, valueExpression)
                    : Expression.NotEqual(valuePropertyAccess, valueExpression);
            case FilterOperators.Gt:
                return Expression.GreaterThan(valuePropertyAccess, valueExpression);
            case FilterOperators.Lt:
                return Expression.LessThan(valuePropertyAccess, valueExpression);
            case FilterOperators.Gte:
                return Expression.GreaterThanOrEqual(valuePropertyAccess, valueExpression);
            case FilterOperators.Lte:
                return Expression.LessThanOrEqual(valuePropertyAccess, valueExpression);
            case FilterOperators.Like:
            case FilterOperators.ILike:
                MethodInfo containsMethod = typeof(string).GetMethod(
                    name: nameof(string.Contains),
                    bindingAttr: BindingFlags.Instance | BindingFlags.Public,
                    types: [typeof(string)])!;

                MethodCallExpression containsExpression;
                if (Operator == FilterOperators.Like)
                {
                    containsExpression = Expression.Call(valuePropertyAccess, containsMethod, arguments: [valueExpression]);
                }
                else
                {
                    MethodInfo toLowerMethod = typeof(string).GetMethod(
                        name: nameof(string.ToLower),
                        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
                        types: Type.EmptyTypes)!;
                    MethodCallExpression propertyToLower = Expression.Call(valuePropertyAccess, toLowerMethod);

                    ConstantExpression lowerValueExpression = Expression.Constant(Value?.ToString()?.ToLower(), propertyType);
                    containsExpression = Expression.Call(propertyToLower, containsMethod, arguments: [lowerValueExpression]);
                }

                return Expression.Equal(containsExpression, Expression.Constant(true));

            default:
                throw new InvalidOperationException($"Field '{Property.Name}' does not support operator {Operator}.");
        }
    }
}
