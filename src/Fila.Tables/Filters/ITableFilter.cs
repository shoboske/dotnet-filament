using Fila.Support;

namespace Fila.Tables.Filters;

/// <summary>One option in a filter's dropdown — Filament's Forms\Components\Select options
/// array, but always resolved eagerly (a filter's option list is small and static; nothing in
/// Fila.Tables needs the lazy/searchable option loading Select itself supports).</summary>
public readonly record struct FilterOption(string Value, string Label);

/// <summary>Non-generic view of a filter, used by rendering code that doesn't know TEntity —
/// mirrors ITableColumn/IRowAction's split from their generic owners. A filter's query-mutating
/// side (Apply) stays on the generic <see cref="TableFilter{TEntity}"/> only, since it needs
/// TEntity to touch IQueryable&lt;TEntity&gt;; nothing non-generic ever calls it.</summary>
public interface ITableFilter
{
    /// <summary>Unique within a table — Filament's BaseFilter name, also the query-string key
    /// this filter's value round-trips under (see TableQuery's filter_&lt;name&gt; convention).</summary>
    string Name { get; }

    string ResolveLabel(EvaluationContext context);

    /// <summary>The dropdown's unselected/blank option label — Filament's HasPlaceholder.</summary>
    string? ResolvePlaceholder(EvaluationContext context);

    /// <summary>The non-blank options this filter's &lt;select&gt; offers, in display order.</summary>
    IReadOnlyList<FilterOption> ResolveOptions(EvaluationContext context);

    /// <summary>True while <paramref name="value"/> represents an active (non-default) state —
    /// what the trigger button's badge counts. Filament's BaseFilter::getIndicators(), reduced
    /// to a bool since nothing in Fila renders the separate "Active filters: ..." indicator
    /// strip Filament shows below the toolbar.</summary>
    bool IsActive(string? value);
}

/// <summary>Marker interface so Resource&lt;TEntity&gt;.ListAsync can tell whether a table
/// carries a TrashedFilter without depending on TrashedFilter&lt;TEntity&gt;'s closed generic
/// type (Resource&lt;TEntity&gt; is itself generic and unconstrained, so it can't reference
/// TrashedFilter&lt;TEntity&gt; by name — see that class's remarks) or on Fila.Support's
/// ISoftDeletable, which Fila.Tables has no reason to know about otherwise.</summary>
public interface ITrashedFilter : ITableFilter;
