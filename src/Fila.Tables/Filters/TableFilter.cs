using Fila.Support;

namespace Fila.Tables.Filters;

/// <summary>Base class for every table filter, and the type a filter collection is declared in
/// terms of — Filament's Filters\BaseFilter. Subclass <see cref="TernaryFilter{TEntity}"/>
/// rather than this one for a three-state (yes/no/blank) filter; a plain single-select filter
/// over an arbitrary column (Filament's SelectFilter) has no caller in Fila yet and isn't ported
/// here — add it if a resource actually needs one, following the same shape as TernaryFilter.</summary>
public abstract class TableFilter<TEntity> : ITableFilter
{
    protected TableFilter(string name)
    {
        Name = name;
    }

    public string Name { get; }

    private protected Evaluated<string> LabelValue { get; set; }

    private protected Evaluated<string?> PlaceholderValue { get; set; } = (string?)null;

    public string ResolveLabel(EvaluationContext context) =>
        LabelValue.Resolve(context) is { Length: > 0 } label ? label : ComponentText.Humanize(Name);

    public string? ResolvePlaceholder(EvaluationContext context) => PlaceholderValue.Resolve(context);

    public abstract IReadOnlyList<FilterOption> ResolveOptions(EvaluationContext context);

    public abstract bool IsActive(string? value);

    /// <summary>Narrows <paramref name="source"/> to what this filter's current <paramref
    /// name="value"/> selects — null/blank means "no restriction", the same convention Table
    /// column search/sort already use for "unset".</summary>
    public abstract IQueryable<TEntity> Apply(IQueryable<TEntity> source, string? value);
}
