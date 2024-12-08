using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq.Expressions;
using System.Reflection;

namespace SchulCloud.RestApi.Filtering;

/// <summary>
/// Provides automated filtering for returned collections.
/// </summary>
/// <typeparam name="TItem">The type of the collection's item.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class FilteringFilterAttribute<TItem> : ActionFilterAttribute
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
    public FilteringFilterAttribute()
    {
        Order = 8;
    }

    /// <inheritdoc/>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _filters.Clear();

        List<string> errors = [];
        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<FilteringFilterAttribute<TItem>>>();

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
                    errors.Add($"Unable to find the field to sort by '{propertyName}'.");
                    continue;
                }

                FilterOperators @operator;
                if (operatorStart != -1)
                {
                    int operatorEnd = parts[0].IndexOf(']');
                    if (operatorEnd <= operatorStart)
                    {
                        SetParameterProblemResponse(context, ["Parameter does not match the expected syntax."]);
                        return;
                    }

                    string operatorStr = parts[0][(operatorStart + 1)..operatorEnd];
                    if (!Enum.TryParse(operatorStr, ignoreCase: true, out @operator))
                    {
                        errors.Add($"Filter operator '{operatorStr}' does not exists.");
                        continue;
                    }

                    if (!IsOperatorAllowed(property.PropertyType, @operator))
                    {
                        errors.Add($"The operator {@operator} cannot be applied to the field '{propertyName}' due to the field type.");
                        continue;
                    }
                }
                else
                {
                    @operator = FilterOperators.Eq;
                }

                if (!TryParseValue(property.PropertyType, parts[1], out object? value))
                {
                    errors.Add($"The provided value for field '{propertyName}' to filter could not be parsed.");
                    continue;
                }

                _filters.Add(new(property, @operator, value));
            }
            else
            {
                SetParameterProblemResponse(context, ["Parameter does not match the expected syntax."]);
                return;
            }
        }

        if (errors.Count > 0)
        {
            SetParameterProblemResponse(context, [.. errors]);
            logger.LogTrace("Request failed by providing invalid data for the filtering parameter.");
        }
        else if (MaxConditions != -1 && _filters.Count > MaxConditions)
        {
            SetProblemResponse(
                context: context,
                title: "Too many filters",
                detail: $"The maximum amount of filters for this endpoint has been exceeded.",
                extensions: new() { { "MaxFilters", MaxConditions } });
        }
    }

    /// <inheritdoc/>
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<FilteringFilterAttribute<TItem>>>();

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

                logger.LogInformation("Filtering proceeded using IQueryable.");
            }
            else if (objectResult.Value is IEnumerable<TItem> collection)
            {
                Func<TItem, bool> predicate = expressionPredicate.Compile();

                collection = collection.Where(predicate);
                objectResult.Value = collection;

                logger.LogInformation("Filtering proceeded on server.");
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

    private static void SetParameterProblemResponse(ActionExecutingContext context, string[] errors)
    {
        Dictionary<string, object?> extensions = new()
        {
            { "errors", new Dictionary<string, string[]> { { "filter", errors } } }
        };
        SetProblemResponse(
            context: context,
            title: "Invalid parameter",
            detail: "The query parameter 'filter' corresponding for filtering is invalid.",
            extensions: extensions);
    }

    private static void SetProblemResponse(ActionExecutingContext context, string title, string detail, Dictionary<string, object?>? extensions = null)
    {
        ControllerBase controller = (ControllerBase)context.Controller;
        context.Result = controller.Problem(
            title: title,
            statusCode: 400,
            detail: detail,
            extensions: extensions);
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

    private static bool TryParseValue(Type type, string value, out object? result)
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
            result = value;
            return true;
        }
        else if (nullable && string.IsNullOrEmpty(value))
        {
            result = null;
            return true;
        }
        else
        {

            MethodInfo? parseMethod = type.GetMethod(
                name: "TryParse",
                bindingAttr: BindingFlags.Static | BindingFlags.Public,
                types: [typeof(string), type]);
            if (parseMethod is not null)
            {
                object?[] parameters = [value, null];
                if ((bool)parseMethod.Invoke(null, parameters)!)
                {
                    result = parameters[1];
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
