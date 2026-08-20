using Microsoft.EntityFrameworkCore.Metadata;

namespace Fila.Tooling.FileGenerators;

/// <summary>Turns EF Core IModel metadata into relation manager source text — backs
/// `make:relation-manager`. Reuses ResourceClassGenerator's own column inference
/// (PlanForProperty) rather than duplicating it — a relation manager's Table() wants exactly
/// the same "what kind of column is this property" answer a resource's own Table() does.</summary>
public static class RelationManagerClassGenerator
{
    public sealed record GeneratedFile(string Source, int ColumnCount);

    /// <summary>Null when TRelated has no foreign key back to TParent — a straightforward
    /// one-to-many is the only shape a relation manager supports (see RelationManager's own doc
    /// comment), so there is nothing to scaffold a Scope() clause against otherwise.</summary>
    public static GeneratedFile? Generate(IEntityType parentType, IEntityType relatedType, string rootNamespace)
    {
        var foreignKey = relatedType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == parentType.ClrType);
        if (foreignKey is null) return null;

        var parentName = parentType.ClrType.Name;
        var relatedName = relatedType.ClrType.Name;
        var relatedParam = char.ToLowerInvariant(relatedName[0]).ToString();
        var parentPk = parentType.FindPrimaryKey()!.Properties[0].Name;
        var relatedFk = foreignKey.Properties[0].Name;

        var columns = new List<string>();
        foreach (var property in relatedType.GetProperties())
        {
            if (columns.Count >= ResourceClassGenerator.MaxColumns) break;
            if (ResourceClassGenerator.ShouldSkip(property)) continue;

            var plan = ResourceClassGenerator.PlanForProperty(property, relatedParam);
            if (plan is not null) columns.Add(plan);
        }

        var columnsBlock = columns.Count > 0
            ? string.Join(",\n            ", columns)
            : "// TODO: add columns — see samples/Demo's OrdersRelationManager.";

        var parentNamespace = parentType.ClrType.Namespace;
        var relatedNamespace = relatedType.ClrType.Namespace;
        var entityNamespaces = new[] { parentNamespace, relatedNamespace }
            .Where(ns => ns is not null && ns != $"{rootNamespace}.Fila.RelationManagers")
            .Distinct()
            .Select(ns => $"using {ns};{Environment.NewLine}");
        var usingEntityNamespaces = string.Concat(entityNamespaces);

        var source = $$"""
            using Fila.Panels.RelationManagers;
            using Fila.Tables;
            {{usingEntityNamespaces}}
            namespace {{rootNamespace}}.Fila.RelationManagers;

            public sealed class {{relatedName}}RelationManager : RelationManager<{{parentName}}, {{relatedName}}>
            {
                protected override Table<{{relatedName}}> Table(Table<{{relatedName}}> t) => t
                    .Columns(
                        {{columnsBlock}})
                    .PaginateBy(10);

                protected override IQueryable<{{relatedName}}> Scope({{parentName}} parent, IQueryable<{{relatedName}}> q) =>
                    q.Where({{relatedParam}} => {{relatedParam}}.{{relatedFk}} == parent.{{parentPk}});
            }

            """;

        return new GeneratedFile(source, columns.Count);
    }
}
