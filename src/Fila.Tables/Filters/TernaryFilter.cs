using Fila.Support;

namespace Fila.Tables.Filters;

/// <summary>A three-state filter — Filament's Filters\TernaryFilter. Renders as a single
/// &lt;select&gt; with a blank/placeholder option plus a "true" and "false" option; which query
/// each state runs is supplied via <see cref="Queries"/>, exactly like Filament's own
/// ->trueQuery()/->falseQuery()/blank fallback. "true"/"false" carry no inherent meaning here —
/// TrashedFilter below repurposes them as "with trashed"/"only trashed" rather than a literal
/// boolean column, the same repurposing Filament's own TrashedFilter does.</summary>
public class TernaryFilter<TEntity> : TableFilter<TEntity>
{
    private Func<IQueryable<TEntity>, IQueryable<TEntity>>? _trueQuery;
    private Func<IQueryable<TEntity>, IQueryable<TEntity>>? _falseQuery;
    private Func<IQueryable<TEntity>, IQueryable<TEntity>>? _blankQuery;

    private protected Evaluated<string> TrueLabelValue { get; set; } = "Yes";
    private protected Evaluated<string> FalseLabelValue { get; set; } = "No";

    public TernaryFilter(string name) : base(name)
    {
        PlaceholderValue = "-";
    }

    public TernaryFilter<TEntity> Label(string label)
    {
        LabelValue = label;
        return this;
    }

    public TernaryFilter<TEntity> Placeholder(string placeholder)
    {
        PlaceholderValue = placeholder;
        return this;
    }

    public TernaryFilter<TEntity> TrueLabel(string label)
    {
        TrueLabelValue = label;
        return this;
    }

    public TernaryFilter<TEntity> FalseLabel(string label)
    {
        FalseLabelValue = label;
        return this;
    }

    /// <summary>Filament's ->queries(true:, false:, blank:) — the query each of this filter's
    /// three states runs. <paramref name="blank"/> defaults to "no restriction" (the source
    /// unchanged), matching Filament's own default when it isn't supplied.</summary>
    public TernaryFilter<TEntity> Queries(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> whenTrue,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> whenFalse,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? whenBlank = null)
    {
        _trueQuery = whenTrue;
        _falseQuery = whenFalse;
        _blankQuery = whenBlank;
        return this;
    }

    public override IReadOnlyList<FilterOption> ResolveOptions(EvaluationContext context) =>
    [
        new FilterOption("1", TrueLabelValue.Resolve(context)),
        new FilterOption("0", FalseLabelValue.Resolve(context)),
    ];

    public override bool IsActive(string? value) => value is "1" or "0";

    public override IQueryable<TEntity> Apply(IQueryable<TEntity> source, string? value) => value switch
    {
        "1" => _trueQuery?.Invoke(source) ?? source,
        "0" => _falseQuery?.Invoke(source) ?? source,
        _ => _blankQuery?.Invoke(source) ?? source,
    };
}
