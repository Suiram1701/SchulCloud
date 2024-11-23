using System.Linq.Expressions;

namespace SchulCloud.RestApi.Linq;

/// <summary>
/// An expression visitor that indicates whether an expression is ordered or not.
/// </summary>
public class OrderedVisitor : ExpressionVisitor
{
    /// <summary>
    /// Indicates whether the expression is ordered.
    /// </summary>
    public bool IsOrdered { get; set; }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Queryable))
        {
            if (node.Method.Name == nameof(Queryable.OrderBy) || node.Method.Name == nameof(Queryable.OrderByDescending))
            {
                IsOrdered = true;
            }
        }

        return base.VisitMethodCall(node);
    }
}
