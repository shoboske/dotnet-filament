using Fila.Panels.Resources;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Fila.Tooling;

/// <summary>Turns EF Core IModel metadata into resource source text. Emits a plain interpolated
/// string rather than pulling in Roslyn SyntaxFactory — the output is small and templating is
/// more readable and easier to change. Scaffolding is a starting point, not a finished screen.</summary>
public static class Scaffolder
{
    private const int MaxColumns = 6;
    private static readonly string[] MoneyNames = ["Total", "Price", "Amount", "Cost"];

    public sealed record ScaffoldResult(string Source, int ColumnCount, string RouteSlug);

    public static ScaffoldResult Scaffold(IEntityType entityType, string rootNamespace, string panelPath)
    {
        var clrType = entityType.ClrType;
        var entityName = clrType.Name;
        var paramName = char.ToLowerInvariant(entityName[0]).ToString();

        var columns = new List<string>();
        var includes = new List<string>();

        foreach (var property in entityType.GetProperties())
        {
            if (columns.Count >= MaxColumns) break;
            if (ShouldSkip(property)) continue;

            var plan = PlanForProperty(property, paramName);
            if (plan is not null) columns.Add(plan);
        }

        foreach (var navigation in entityType.GetNavigations().Where(n => !n.IsCollection))
        {
            if (columns.Count >= MaxColumns) break;

            var targetType = navigation.TargetEntityType.ClrType;
            var displayProperty = targetType.GetProperties()
                .FirstOrDefault(p => p.PropertyType == typeof(string));
            if (displayProperty is null) continue;

            columns.Add(
                $"t.Text({paramName} => {paramName}.{navigation.Name}.{displayProperty.Name}).Label(\"{navigation.Name}\")");
            includes.Add($".Include({paramName} => {paramName}.{navigation.Name})");
        }

        var dateProperty = entityType.GetProperties()
            .FirstOrDefault(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?) ||
                                  p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?));

        var defaultSortLine = dateProperty is not null
            ? $"\n        .DefaultSort({paramName} => {paramName}.{dateProperty.Name}, descending: true)"
            : string.Empty;

        var queryMethod = includes.Count > 0
            ? $"\n\n    protected override IQueryable<{entityName}> Query(IQueryable<{entityName}> q) => q{string.Concat(includes)};"
            : string.Empty;

        var entityNamespace = clrType.Namespace;
        var usingEntityNamespace = entityNamespace is null || entityNamespace == $"{rootNamespace}.Fila.Resources"
            ? string.Empty
            : $"using {entityNamespace};{Environment.NewLine}";

        var columnsBlock = string.Join(",\n            ", columns);

        var source = $$"""
            using Fila.Panels.Resources;
            using Fila.Tables;
            using Microsoft.EntityFrameworkCore;
            {{usingEntityNamespace}}
            namespace {{rootNamespace}}.Fila.Resources;

            public sealed class {{entityName}}Resource : Resource<{{entityName}}>
            {
                protected override Table<{{entityName}}> Table(Table<{{entityName}}> t) => t
                    .Columns(
                        {{columnsBlock}}){{defaultSortLine}}
                    .PaginateBy(25);{{queryMethod}}
            }

            """;

        var slug = ResourceNaming.ToSlug(entityName);
        return new ScaffoldResult(source, columns.Count, $"/{panelPath}/{slug}");
    }

    private static bool ShouldSkip(IProperty property)
    {
        if (property.IsShadowProperty()) return true;
        if (property.IsPrimaryKey()) return true;
        if (property.IsForeignKey()) return true;
        if (property.IsConcurrencyToken) return true;
        if (property.ClrType == typeof(byte[])) return true;

        var name = property.Name;
        if (name.Equals("Id", StringComparison.Ordinal) || name.EndsWith("Id", StringComparison.Ordinal))
            return true;

        return false;
    }

    private static string? PlanForProperty(IProperty property, string paramName)
    {
        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        var access = $"{paramName}.{property.Name}";

        if (clrType == typeof(string))
            return $"t.Text({paramName} => {access}).Searchable().Sortable()";

        if (clrType == typeof(bool))
            return $"t.Bool({paramName} => {access})";

        if (clrType == typeof(DateTime) || clrType == typeof(DateTimeOffset))
            return $"t.Date({paramName} => {access}).Sortable()";

        if (clrType.IsEnum)
            return $"t.Badge({paramName} => {access})";

        if (clrType == typeof(decimal) && IsMoneyName(property.Name))
            return $"t.Money({paramName} => {access}).Sortable().Alignment(ColumnAlign.End)";

        if (IsNumeric(clrType))
            return $"t.Text({paramName} => {access}).Sortable().Alignment(ColumnAlign.End)";

        return null;
    }

    private static bool IsMoneyName(string name) =>
        MoneyNames.Any(m => name.Contains(m, StringComparison.OrdinalIgnoreCase));

    private static bool IsNumeric(Type type) =>
        type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) ||
        type == typeof(float) || type == typeof(double) || type == typeof(decimal) ||
        type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);
}
