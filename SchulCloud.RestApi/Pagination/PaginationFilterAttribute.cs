using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SchulCloud.RestApi.Linq;
using SchulCloud.RestApi.Models;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace SchulCloud.RestApi.Pagination;

/// <summary>
/// A filter that applies pagination to collection results. The default page size is 100.
/// </summary>
/// <typeparam name="TItem"></typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class PaginationFilterAttribute<TItem> : ActionFilterAttribute
{
    /// <summary>
    /// The offset from the collection to use.
    /// </summary>
    public int Offset { get; init; } = 0;

    /// <summary>
    /// The maximum amount of items to return per request.
    /// </summary>
    public int Limit { get; init; } = 100;

    /// <summary>
    /// Status codes where the filter should be applied on.
    /// </summary>
    /// <remarks>
    /// If <c>null</c> the filter will be applied on every code between 200...299.
    /// </remarks>
    public int[]? StatusCodes { get; set; }

    private int _offset;
    private int _limit;

    private readonly static MethodInfo _orderMethod;

    /// <summary>
    /// Creates a new instance
    /// </summary>
    public PaginationFilterAttribute()
    {
        Order = 10;
    }

    static PaginationFilterAttribute()
    {
        _orderMethod = typeof(PaginationFilterAttribute<TItem>).GetMethod(
            name: nameof(OrderBy),
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
            types: [typeof(IQueryable<TItem>), typeof(string)])!;
    }

    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Dictionary<string, string> errors = [];
        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<PaginationFilterAttribute<TItem>>>();

        string? offsetHeader = context.HttpContext.Request.Query["offset"];
        if (int.TryParse(offsetHeader, out _offset) && _offset >= 0)
        {
        }
        else if (string.IsNullOrEmpty(offsetHeader))
        {
            _offset = Offset;
        }
        else
        {
            errors.Add("offset", "An integer value greater or same than 0 was expected.");
            logger.LogTrace("Request failed by providing an invalid 'offset' parameter for pagination.");
        }

        string? limitHeader = context.HttpContext.Request.Query["limit"];
        if (int.TryParse(limitHeader, out _limit) && _limit >= 1)
        {
        }
        else if (string.IsNullOrEmpty(limitHeader))
        {
            _limit = Limit;
        }
        else
        {
            errors.Add("limit", "An integer value greater or same than 1 was expected.");
            logger.LogTrace("Request failed by providing an invalid 'limit' parameter for pagination.");
        }

        SetParameterProblemResponse(context, errors);
    }

    /// <inheritdoc />
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<PaginationFilterAttribute<TItem>>>();

        if (context.Result is ObjectResult objectResult && AllowedStatusCode(objectResult.StatusCode ?? 200))
        {
            TItem[] items;
            int totalItemCount;
            if (objectResult.Value is IQueryable<TItem> queryCollection)
            {
                // EF Core recommends to sort a query if not already done.
                OrderedVisitor visitor = new();
                visitor.Visit(queryCollection.Expression);
                if (!visitor.IsOrdered)
                {
                    PropertyInfo[] properties = typeof(TItem).GetProperties();
                    PropertyInfo orderProperty = properties.FirstOrDefault(p => p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                        ?? properties.FirstOrDefault(p => p.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
                        ?? properties.First();
                    queryCollection = OrderBy(queryCollection, orderProperty);

                    logger.LogInformation("Applied sort by '{property}' automatically.", orderProperty.Name);
                }

                totalItemCount = await queryCollection.CountAsync().ConfigureAwait(false);
                if (totalItemCount > 0)     // If its clear that there isn't any item to get the 'real' query is unnecessary.
                {
                    items = await queryCollection
                        .Skip(_offset)
                        .Take(_limit)
                        .ToArrayAsync().ConfigureAwait(false);
                }
                else
                {
                    items = [];
                }

                logger.LogInformation("Pagination proceeded using IQueryable.");
            }
            else if (objectResult.Value is IEnumerable<TItem> collection)
            {
                items = collection
                    .Skip(_offset)
                    .Take(_limit)
                    .ToArray();
                totalItemCount = collection.Count();

                logger.LogInformation("Pagination proceeded on server.");
            }
            else
            {
                logger.LogError("Unable to cast type '{type}' into the pageable type '{supportedType}'.", objectResult.Value?.GetType(), typeof(IEnumerable<TItem>));
                await base.OnResultExecutionAsync(context, next).ConfigureAwait(false);
                return;
            }

            PagingInfo<TItem> paging = new()
            {
                Items = items,
                Offset = _offset,
                Limit = _limit,
                TotalItems = totalItemCount
            };
            objectResult.Value = paging;
        }

        await base.OnResultExecutionAsync(context, next).ConfigureAwait(false);
    }

    private static void SetParameterProblemResponse(ActionExecutingContext context, IDictionary<string, string> errors)
    {
        if (errors.Count > 0)
        {
            Dictionary<string, object?> extensions = new() { { "errors", errors } };

            ControllerBase controller = (ControllerBase)context.Controller;
            context.Result = controller.Problem(
                title: "Invalid parameters",
                statusCode: 400,
                detail: "One or more query parameters corresponding for pagination were invalid.",
                extensions: extensions);
        }
    }

    private bool AllowedStatusCode(int statusCode)
    {
        if (StatusCodes is not null)
        {
            return StatusCodes.Contains(statusCode);
        }

        return statusCode >= 200 && statusCode < 300;
    }
  
    private static IOrderedQueryable<TItem> OrderBy(IQueryable<TItem> queryable, PropertyInfo property)
    {
        MethodInfo orderMethod = _orderMethod.MakeGenericMethod(property.PropertyType);
        return (IOrderedQueryable<TItem>)orderMethod.Invoke(null, parameters: [queryable, property.Name])!;
    }

    private static IOrderedQueryable<TItem> OrderBy<TProperty>(IQueryable<TItem> queryable, string propertyName)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TItem), "o");
        MemberExpression propertyAccess = Expression.Property(parameter, propertyName);
        Expression<Func<TItem, TProperty>> expression = Expression.Lambda<Func<TItem, TProperty>>(propertyAccess, parameter);

        return queryable.OrderBy(expression);
    }
}
