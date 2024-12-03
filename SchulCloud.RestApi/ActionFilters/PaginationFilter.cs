using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SchulCloud.RestApi.Linq;
using SchulCloud.RestApi.Models;
using System.Collections;
using System.Linq.Expressions;

namespace SchulCloud.RestApi.ActionFilters;

/// <summary>
/// A filter that applies pagination to collection results. The default page size is 100.
/// </summary>
/// <typeparam name="TItem"></typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class PaginationFilter<TItem> : ActionFilterAttribute
{
    /// <summary>
    /// The default page.
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// The default page size.
    /// </summary>
    public int PageSize { get; set; } = 100;

    /// <summary>
    /// Status codes where the filter should be applied on.
    /// </summary>
    /// <remarks>
    /// If <c>null</c> the filter will be applied on every code between 200...299.
    /// </remarks>
    public int[]? StatusCodes { get; set; }

    /// <summary>
    /// Creates a new instance
    /// </summary>
    public PaginationFilter()
    {
        Order = 10;
    }

    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        string? pageHeader = context.HttpContext.Request.Query["page"];
        if (string.IsNullOrEmpty(pageHeader))
        {
        }
        else if (int.TryParse(pageHeader, out int page) && page >= 0)
        { 
            PageIndex = page;
        }
        else
        {
            context.Result = GetProblemResult(context, "The query parameter 'page' have to be an integer greater or same than 0.");
        }

        string? pageSizeHeader = context.HttpContext.Request.Query["pageSize"];
        if (string.IsNullOrEmpty(pageSizeHeader))
        {
        }
        else if (int.TryParse(pageSizeHeader, out int pageSize) && pageSize >= 1)
        {
            PageSize = pageSize;
        }
        else
        {
            context.Result = GetProblemResult(context, "The query parameter 'pageSize' have to be an integer greater or same than 1.");
        }
    }

    /// <inheritdoc />
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<PaginationFilter<TItem>>>();

        if (context.Result is ObjectResult objectResult && AllowedStatusCode(objectResult.StatusCode ?? 200))
        {
            TItem[] items;
            int totalItemCount;
            if (objectResult.Value is IQueryable<TItem> queryCollection)
            {
                OrderedVisitor visitor = new();
                visitor.Visit(queryCollection.Expression);
                if (!visitor.IsOrdered)
                {
                    Expression<Func<TItem, string>> expression = CreateIdAccessExpression();
                    queryCollection = queryCollection.OrderBy(expression);
                }

                items = await queryCollection
                    .Skip(PageIndex * PageSize)
                    .Take(PageSize)
                    .ToArrayAsync().ConfigureAwait(false);
                totalItemCount = await queryCollection.CountAsync().ConfigureAwait(false);
            }
            else if (objectResult.Value is IEnumerable<TItem> collection)
            {
                items = collection
                    .Skip(PageIndex * PageSize)
                    .Take(PageSize)
                    .ToArray();
                totalItemCount = collection.Count();
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
                Page = PageIndex,
                PageSize = PageSize,
                TotalItems = totalItemCount,
                TotalPages = (int)Math.Ceiling((float)totalItemCount / PageSize)
            };
            objectResult.Value = paging;
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

    private static Expression<Func<TItem, string>> CreateIdAccessExpression()
    {
        Type idType = typeof(TItem).GetProperty("Id")?.PropertyType
            ?? throw new InvalidOperationException("Unable to find a Id member to sort the queryable collection.");
        if (idType != typeof(string))
        {
            throw new InvalidOperationException("The id have to be an string");
        }

        ParameterExpression parameter = Expression.Parameter(typeof(TItem), "model");
        MemberExpression propertyAccess = Expression.Property(parameter, "Id");

        return Expression.Lambda<Func<TItem, string>>(propertyAccess, parameter);
    }
}
