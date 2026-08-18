using System.Linq.Expressions;

namespace Fila.Support;

/// <summary>
/// Turns a property-access lambda (e.g. <c>o =&gt; o.Customer.Name</c>) into a dotted path
/// string, and rebuilds that path against a fresh parameter for use inside an IQueryable.
/// </summary>
public static class ExpressionPath
{
    public static string ToPath<T>(Expression<Func<T, object?>> selector) => ToPath((LambdaExpression)selector);

    public static string ToPath(LambdaExpression selector)
    {
        var body = Unwrap(selector.Body);
        var segments = new List<string>();

        while (body is MemberExpression member)
        {
            segments.Add(member.Member.Name);
            body = Unwrap(member.Expression ?? throw NotSupported(selector));
        }

        if (body is not ParameterExpression || segments.Count == 0)
            throw NotSupported(selector);

        segments.Reverse();
        return string.Join('.', segments);
    }

    /// <summary>
    /// Rebuilds "Customer.Name" as (T param) => param.Customer.Name, typed as TValue.
    /// </summary>
    public static LambdaExpression Rebuild(Type entityType, string path, Type? valueType = null)
    {
        var parameter = Expression.Parameter(entityType, "e");
        Expression body = parameter;

        foreach (var segment in path.Split('.'))
            body = Expression.PropertyOrField(body, segment);

        if (valueType is not null && body.Type != valueType)
            body = Expression.Convert(body, valueType);

        return Expression.Lambda(body, parameter);
    }

    public static Type ResolvePathType(Type entityType, string path)
    {
        Type current = entityType;
        foreach (var segment in path.Split('.'))
        {
            var prop = current.GetProperty(segment)
                ?? throw new NotSupportedException($"Property '{segment}' not found on '{current.Name}' while resolving path '{path}'.");
            current = prop.PropertyType;
        }
        return current;
    }

    private static Expression Unwrap(Expression e) =>
        e is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u
            ? u.Operand
            : e;

    private static NotSupportedException NotSupported(LambdaExpression selector) =>
        new($"Fila columns only support simple property paths (e.g. o => o.Customer.Name). " +
            $"Unsupported expression: '{selector}'.");
}
