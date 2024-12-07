using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SchulCloud.RestApi.ActionFilters;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace SchulCloud.RestApi.Filtering;

/// <summary>
/// Provides automated filtering for returned collections.
/// </summary>
/// <typeparam name="TItem">The type of the collection's item.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class FilteringFilter<TItem> : ActionFilterAttribute
    where TItem : class
{
    /// <summary>
    /// The maximum allowed amount of conditions per request.
    /// </summary>
    /// <remarks>
    /// By default set to 10. -1 means that its unlimited (not recommended).
    /// </remarks>
    public int MaxConditions { get; set; } = 10;

    /// <summary>
    /// Status codes where the filter should be applied on.
    /// </summary>
    /// <remarks>
    /// If <c>null</c> the filter will be applied on every code between 200...299.
    /// </remarks>
    public int[]? StatusCodes { get; set; }

    private readonly List<FilterItem<TItem>> _filters = [];

    /// <summary>
    /// Creates a new instance
    /// </summary>
    public FilteringFilter()
    {
        Order = 8;
    }

    /// <inheritdoc/>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _filters.Clear();

        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<FilteringFilter<TItem>>>();

        IEnumerable<string> filters = context.HttpContext.Request.Query
            .Where(param => param.Key == "filter")
            .SelectMany(param => param.Value)
            .SelectMany(param => param?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? []);
        foreach (string filter in filters)
        {
            string[] parts = filter.Split(':');
            if (parts.Length == 2)
            {
                int operatorStart = parts[0].IndexOf('[');

                string propertyName = operatorStart != -1
                    ? parts[0][..operatorStart]
                    : parts[0];
                PropertyInfo? property = typeof(TItem)
                    .GetProperties()
                    .SingleOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) && !p.IsSpecialName);
                if (property is null)
                {
                    context.Result = GetProblemResult(context, $"Unable to find the field to sort by '{propertyName}'.");
                    return;
                }

                FilterOperators @operator;
                if (operatorStart != -1)
                {
                    int operatorEnd = parts[0].IndexOf(']');
                    if (operatorEnd <= operatorStart)
                    {
                        context.Result = GetProblemResult(context, "Invalid format");
                        return;
                    }

                    string operatorStr = parts[0][(operatorStart + 1)..operatorEnd];
                    if (!Enum.TryParse(operatorStr, ignoreCase: true, out @operator))
                    {
                        context.Result = GetProblemResult(context, $"Filter operator '{operatorStr}' does not exists.");
                        return;
                    }

                    if (!IsOperatorAllowed(property.PropertyType, @operator))
                    {
                        context.Result = GetProblemResult(context, $"The operator {@operator} cannot be applied to the field '{propertyName}' due to the field type.");
                        return;
                    }
                }
                else
                {
                    @operator = FilterOperators.Eq;
                }

                object? value;
                try
                {
                    value = ParseValue(property.PropertyType, parts[1]);
                }
                catch (FormatException fe)
                {
                    context.Result = GetProblemResult(context, $"The provided value for field '{propertyName}' to filter could not be parsed. {fe.Message}");
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An unexpected exception occurred while parsing value '{value}' into type '{type}'.", parts[1], property.PropertyType);
                    context.Result = GetProblemResult(context, $"Unable to parse the provided value for field '{propertyName}' to filter for.");
                    return;
                }

                _filters.Add(new(property, @operator, value));
            }
            else
            {
                context.Result = GetProblemResult(context, "Invalid filter syntax.");
            }
        }

        if (MaxConditions != -1 && _filters.Count > MaxConditions)
        {
            context.Result = GetProblemResult(context, $"This endpoint has a maximum of {MaxConditions} filters which has been exceeded.");
        }
    }

    /// <inheritdoc/>
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<FilteringFilter<TItem>>>();

        if (context.Result is ObjectResult objectResult && AllowedStatusCode(objectResult.StatusCode ?? 200) && _filters.Count > 0)
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(TItem), "o");

            Expression expression = _filters[0].BuildExpression(parameterExpression);
            foreach (FilterItem<TItem> filter in _filters.Skip(1))     // Concat every filter requirement with an AND
            {
                BinaryExpression filterExpression = filter.BuildExpression(parameterExpression);
                expression = Expression.And(expression, filterExpression);
            }

            Expression<Func<TItem, bool>> expressionPredicate = Expression.Lambda<Func<TItem, bool>>(expression, parameters: [parameterExpression]);
            if (objectResult.Value is IQueryable<TItem> query)
            {
                query = query.Where(expressionPredicate);
                objectResult.Value = query;
            }
            else if (objectResult.Value is IEnumerable<TItem> collection)
            {
                Func<TItem, bool> predicate = expressionPredicate.Compile();

                collection = collection.Where(predicate);
                objectResult.Value = collection;
            }
            else
            {
                logger.LogError("Unable to cast type '{type}' into the sortable type '{supportedType}'.", objectResult.Value?.GetType(), typeof(IEnumerable<TItem>));
                await base.OnResultExecutionAsync(context, next).ConfigureAwait(false);
                return;
            }
        }

        await base.OnResultExecutionAsync(context, next).ConfigureAwait(false);
    }

    private bool AllowedStatusCode(int statusCode)
    {
        if (StatusCodes is not null)
        {
            return StatusCodes.Contains(statusCode);
        }

        return statusCode >= 200 && statusCode < 300;
    }

    private static ObjectResult GetProblemResult(ActionExecutingContext context, string detail)
    {
        ControllerBase controller = (ControllerBase)context.Controller;
        return controller.Problem(statusCode: 400, detail: detail);
    }

    private static bool IsOperatorAllowed(Type type, FilterOperators @operator)
    {
        switch (@operator)
        {
            case FilterOperators.Eq:
            case FilterOperators.Ne:
                return true;
            case FilterOperators.Gt:
            case FilterOperators.Lt:
            case FilterOperators.Gte:
            case FilterOperators.Lte:
                try
                {
                    // https://stackoverflow.com/questions/8523061/how-to-verify-whether-a-type-overloads-supports-a-certain-operator
                    ConstantExpression constExpression = Expression.Constant(default, type);
                    Expression.Add(constExpression, constExpression);
                    return true;
                }
                catch
                {
                    return false;
                }
            case FilterOperators.Like:
            case FilterOperators.ILike:
                return type == typeof(string);
            default:
                return false;
        }
    }

    private static object? ParseValue(Type type, string value)
    {
        bool nullable = false;
        if (type.GenericTypeArguments.Length > 0)
        {
            Type genericParam = type.GenericTypeArguments[0];
            if (type == typeof(Nullable<>).MakeGenericType(genericParam))
            {
                type = genericParam;
                nullable = true;
            }
        }

        if (type == typeof(string))
        {
            return Uri.UnescapeDataString(value);
        }
        else if (nullable && string.IsNullOrEmpty(value))
        {
            return null;
        }
        else
        {
            MethodInfo? parseMethod = type.GetMethod(
            name: "Parse",
            bindingAttr: BindingFlags.Static | BindingFlags.Public,
            types: [typeof(string)]);
            if (parseMethod is not null)
            {
                return parseMethod.Invoke(null, parameters: [value]);
            }
            else
            {
                throw new Exception();
            }
        }
    }
}
