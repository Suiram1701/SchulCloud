using System.Linq.Expressions;

namespace SchulCloud.RestApi.Linq;

/// <summary>
/// An expression visitor that is be able to update the selector of an Select call.
/// </summary>
/// <param name="updateSelect">The update function. If no changes should be made just return the supplied expression.</param>
public class UpdateSelectVisitor(Func<ParameterExpression[], Expression, Expression> updateSelect) : ExpressionVisitor
{
    /// <summary>
    /// Indicates whether a select were updated.
    /// </summary>
    public bool Visited { get; private set; }

    private bool _visitSelect = false;

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == nameof(Queryable.Select))
        {
            _visitSelect = true;
            Expression newSelector = Visit(node.Arguments[1]);
            _visitSelect = false;
            
            return node.Update(null, [node.Arguments[0], newSelector]);
        }

        return base.VisitMethodCall(node);
    }

    /// <inheritdoc />
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        if (_visitSelect)
        {
            Visited = true;

            Expression newBody = updateSelect([.. node.Parameters], node.Body);
            return node.Update(newBody, node.Parameters);
        }

        return base.VisitLambda(node);
    }
}
