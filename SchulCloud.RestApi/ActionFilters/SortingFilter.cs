using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SchulCloud.RestApi.Linq;
using SchulCloud.RestApi.Models;
using System.Linq;
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

    private static readonly MethodInfo _queryOrderMethod = default!;
    private static readonly MethodInfo _orderMethod = default!;

    /// <summary>
    /// Creates a new instance
    /// </summary>
    public SortingFilter()
    {
        Order = 9;
    }

    static SortingFilter()
    {
        Type classType = typeof(SortingFilter<TItem>);
        Type[] parameters = [typeof(string), typeof(bool), typeof(bool)];

        _queryOrderMethod = classType.GetMethod(
            name: nameof(QueryOrderBy),
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
            types: [typeof(IQueryable<TItem>), .. parameters])!;
        _orderMethod = classType.GetMethod(
            name: nameof(OrderBy),
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
            types: [typeof(IEnumerable<TItem>), .. parameters])!;
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
            SortingDirection sortingDirection = value[0] == '-'     // The first character contains the direction operator
                ? SortingDirection.Desc
                : SortingDirection.Asc;

            string propertyName = value.TrimStart('+', '-', ' ');     // If this runs without ' ' it leaves a white-space instead.

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
            context.Result = GetProblemResult(context, $"This endpoint has a maximum of {MaxSortCriterias} sort criterias which has been exceeded.");
        }
    }

    /// <inheritdoc />
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SortingFilter<TItem>>>();

        if (context.Result is ObjectResult objectResult && AllowedStatusCode(objectResult.StatusCode ?? 200) && _sortings.Count > 0)
        {
            if (objectResult.Value is IQueryable<TItem> queryable)
            {
                SortingCriteria firstCriteria = _sortings[0];     // Only for the first one can OrderBy be used afterward ThenBy have to be used.
                IOrderedQueryable<TItem> orderedQueryable = QueryOrderBy(queryable, firstCriteria.Property, true, firstCriteria.Direction == SortingDirection.Asc);

                foreach (SortingCriteria criteria in _sortings.Skip(1))
                {
                    orderedQueryable = QueryOrderBy(orderedQueryable, criteria.Property, false, criteria.Direction == SortingDirection.Asc);
                }
                objectResult.Value = orderedQueryable;
            }
            else if (objectResult.Value is IEnumerable<TItem> collection)
            {
                SortingCriteria firstCriteria = _sortings[0];
                IOrderedEnumerable<TItem> orderedCollection = OrderBy(collection, firstCriteria.Property, true, firstCriteria.Direction == SortingDirection.Asc);

                foreach (SortingCriteria criteria in _sortings.Skip(1))
                {
                    orderedCollection = OrderBy(orderedCollection, criteria.Property, false, criteria.Direction == SortingDirection.Asc);
                }
                objectResult.Value = orderedCollection;
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

    private static IOrderedQueryable<TItem> QueryOrderBy(IQueryable<TItem> queryable, PropertyInfo property, bool first, bool ascending)
    {
        MethodInfo orderMethod = _queryOrderMethod.MakeGenericMethod(property.PropertyType);
        return (IOrderedQueryable<TItem>)orderMethod.Invoke(null, parameters: [queryable, property.Name, first, ascending])!;
    }

    private static IOrderedQueryable<TItem> QueryOrderBy<TProperty>(IQueryable<TItem> queryable, string propertyName, bool first, bool ascending)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TItem), "o");
        MemberExpression propertyAccess = Expression.Property(parameter, propertyName);
        Expression<Func<TItem, TProperty>> expression = Expression.Lambda<Func<TItem, TProperty>>(propertyAccess, parameter);

        if (first)
        {
            return ascending
                ? queryable.OrderBy(expression)
                : queryable.OrderByDescending(expression);
        }
        else
        {
            IOrderedQueryable<TItem> orderedQueryable = (IOrderedQueryable<TItem>)queryable;
            return ascending
                ? orderedQueryable.ThenBy(expression)
                : orderedQueryable.ThenByDescending(expression);
        }
    }

    private static IOrderedEnumerable<TItem> OrderBy(IEnumerable<TItem> enumerable, PropertyInfo property, bool first, bool ascending)
    {
        MethodInfo orderMethod = _orderMethod.MakeGenericMethod(property.PropertyType);
        return (IOrderedEnumerable<TItem>)orderMethod.Invoke(null, parameters: [enumerable, property.Name, first, ascending])!;
    }

    private static IOrderedEnumerable<TItem> OrderBy<TProperty>(IEnumerable<TItem> enumerable, string propertyName, bool first, bool ascending)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TItem), "o");
        MemberExpression propertyAccess = Expression.Property(parameter, propertyName);
        Expression<Func<TItem, TProperty>> expression = Expression.Lambda<Func<TItem, TProperty>>(propertyAccess, parameter);
        Func<TItem, TProperty> compiledExpression = expression.Compile();

        if (first)
        {
            return ascending
                ? enumerable.OrderBy(compiledExpression)
                : enumerable.OrderByDescending(compiledExpression);
        }
        else
        {
            IOrderedEnumerable<TItem> orderedEnumerable = (IOrderedEnumerable<TItem>)enumerable;
            return ascending
                ? orderedEnumerable.ThenBy(compiledExpression)
                : orderedEnumerable.ThenByDescending(compiledExpression);
        }
    }

    private static LambdaExpression CreatePropertyExpression(PropertyInfo property)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TItem), "o");
        MemberExpression propertyAccess = Expression.Property(parameter, property.Name);

        return Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(TItem), property.PropertyType), propertyAccess, parameter);
    }

    private record SortingCriteria(PropertyInfo Property, SortingDirection Direction);

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
