using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SchulCloud.RestApi.Linq;
using SchulCloud.RestApi.Models;
using System.Linq.Expressions;
using System.Reflection;

namespace SchulCloud.RestApi.ActionFilters;

/// <summary>
/// A filter that applies sorting to a returned collection.
/// </summary>
/// <typeparam name="TItem"></typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SortingFilter<TItem> : ActionFilterAttribute
{
    /// <summary>
    /// The maximum amount of sorting criterias.
    /// </summary>
    /// <remarks>
    /// By default set to 10. -1 means that its unlimited (not recommended).
    /// </remarks>
    public int MaxSortCriterias { get; set; } = 10;

    /// <summary>
    /// Status codes where the filter should be applied on.
    /// </summary>
    /// <remarks>
    /// If <c>null</c> the filter will be applied on every code between 200...299.
    /// </remarks>
    public int[]? StatusCodes { get; set; }

    private readonly List<SortingCriteria> _sortings = [];

    /// <summary>
    /// Creates a new instance
    /// </summary>
    public SortingFilter()
    {
        Order = 9;
    }

    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _sortings.Clear();

        IEnumerable<string> sortings = context.HttpContext.Request.Query
            .Where(param => param.Key == "sort")
            .SelectMany(param => param.Value)
            .SelectMany(param => param?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? []);
        foreach (string value in sortings)
        {
            string propertyName = value.TrimStart('+', '-', ' ');     // If this runs without ' ' it leaves a white-space instead.

            SortingDirection sortingDirection = value[0] == '-'     // The first character contains the direction operator
                ? SortingDirection.Desc
                : SortingDirection.Asc;

            PropertyInfo? property = typeof(TItem).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .SingleOrDefault(property => property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) && !property.IsSpecialName);
            if (property is null)
            {
                context.Result = GetProblemResult(context, $"Unable to find the field to sort by '{propertyName}'.");
                return;
            }

            if (property.PropertyType != typeof(string) && !property.PropertyType.IsEnum && !property.PropertyType.IsValueType)
            {
                context.Result = GetProblemResult(context, $"Field '{propertyName}' is not sortable!");
                return;
            }

            _sortings.Add(new(property, sortingDirection));
        }

        if (MaxSortCriterias != -1 && _sortings.Count > MaxSortCriterias)
        {
            context.Result = GetProblemResult(context, $"This endpoint has a maximum of {MaxSortCriterias} sort criterias.");
        }
    }

    /// <inheritdoc />
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SortingFilter<TItem>>>();

        if (context.Result is ObjectResult objectResult && AllowedStatusCode(objectResult.StatusCode ?? 200) && _sortings.Count > 0)
        {
            Func<object, bool, bool, PropertyInfo, object> order;     // Parameters: object collection, bool isFirst, bool isAscending, PropertyInfo orderBy
            if (objectResult.Value is IQueryable<TItem>)
            {
                Type expressionType = typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(1)));
                order = (collection, first, ascending, property) =>
                {
                    string method = first
                        ? (ascending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending))
                        : (ascending ? nameof(Queryable.ThenBy) : nameof(Queryable.ThenByDescending));
                    Type sourceCollectionType = (first ? typeof(IQueryable<>) : typeof(IOrderedQueryable<>)).MakeGenericType(Type.MakeGenericMethodParameter(0));

                    MethodInfo orderByMethod = typeof(Queryable).GetMethod(method, BindingFlags.Static | BindingFlags.Public, [sourceCollectionType, expressionType])!
                        .MakeGenericMethod(typeof(TItem), property.PropertyType);
                    return orderByMethod.Invoke(null, parameters: [collection, CreatePropertyExpression(property)])!;
                };
            }
            else if (objectResult.Value is IEnumerable<TItem>)
            {
                Type delegateType = typeof(Func<,>).MakeGenericType(Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(1));
                order = (collection, first, ascending, property) =>
                {
                    string method = first
                        ? (ascending ? nameof(Enumerable.OrderBy) : nameof(Enumerable.OrderByDescending))
                        : (ascending ? nameof(Enumerable.ThenBy) : nameof(Enumerable.ThenByDescending));
                    Type sourceCollectionType = (first ? typeof(IEnumerable<>) : typeof(IOrderedEnumerable<>)).MakeGenericType(Type.MakeGenericMethodParameter(0));

                    Type expressionType = typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(typeof(TItem), property.PropertyType));
                    MethodInfo compileExpressionMethod = expressionType.GetMethod("Compile", BindingFlags.Instance | BindingFlags.Public, [])!;
                    object compiledExpression = compileExpressionMethod.Invoke(CreatePropertyExpression(property), null)!;

                    MethodInfo orderByMethod = typeof(Enumerable).GetMethod(method, BindingFlags.Static | BindingFlags.Public, [sourceCollectionType, delegateType])!
                        .MakeGenericMethod(typeof(TItem), property.PropertyType);
                    return orderByMethod.Invoke(null, parameters: [collection, compiledExpression])!;
                };
            }
            else
            {
                logger.LogError("Unable to cast type '{type}' into the sortable type '{supportedType}'.", objectResult.Value?.GetType(), typeof(IEnumerable<TItem>));
                await base.OnResultExecutionAsync(context, next).ConfigureAwait(false);
                return;
            }

            SortingCriteria firstCriteria = _sortings[0];     // Only for the first one can OrderBy be used afterward ThenBy have to be used.
            object orderedCollection = order(objectResult.Value, true, firstCriteria.Direction == SortingDirection.Asc, firstCriteria.OrderBy);

            foreach (SortingCriteria criteria in _sortings.Skip(1))
            {
                orderedCollection = order(orderedCollection, false, criteria.Direction == SortingDirection.Asc, criteria.OrderBy);
            }
            objectResult.Value = orderedCollection;
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

    private static LambdaExpression CreatePropertyExpression(PropertyInfo property)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TItem), "model");
        MemberExpression propertyAccess = Expression.Property(parameter, property.Name);

        return Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(TItem), property.PropertyType), propertyAccess, parameter);
    }

    private record SortingCriteria(PropertyInfo OrderBy, SortingDirection Direction);

    private enum SortingDirection
    {
        /// <summary>
        /// Ascending
        /// </summary>
        Asc,

        /// <summary>
        /// Descending
        /// </summary>
        Desc
    }
}
