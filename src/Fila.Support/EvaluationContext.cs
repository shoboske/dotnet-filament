using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Fila.Support;

/// <summary>Everything a component's configuration closure is allowed to look at: the record
/// being rendered, the request's DbContext, the signed-in user, and the values already entered
/// for the other components in the same schema.
///
/// This is the generalisation of the <c>db =&gt; ...</c> closure that used to be the only
/// supported closure shape (on Select's option list). Any setting can now be computed instead
/// of hardcoded, and they all read from the same context object rather than each setting
/// inventing its own parameter list.
///
/// It lives in the support package for the same reason Filament's EvaluatesClosures does
/// (packages/support/src/Concerns/EvaluatesClosures.php): schema components and table columns
/// both evaluate closures, and neither is built on the other. Filament passes the same set of
/// dependencies by reflecting on the closure's parameter names — see
/// Column::resolveDefaultClosureDependencyForEvaluationByName, which offers record, state,
/// livewire, rowLoop and table. One typed context object is the C# equivalent.</summary>
public sealed class EvaluationContext
{
    private static readonly IReadOnlyDictionary<string, string?> NoState =
        new Dictionary<string, string?>();

    /// <summary>A context with nothing in it — for resolving a value known to be constant.</summary>
    public static readonly EvaluationContext Empty = new();

    /// <summary>The DbContext serving the current request, when there is one.</summary>
    public DbContext? Db { get; init; }

    /// <summary>The entity being edited or the row being rendered. Null where there isn't one
    /// yet — a table header, for instance, is drawn once for the whole page.</summary>
    public object? Record { get; init; }

    public ClaimsPrincipal? User { get; init; }

    /// <summary>Raw (unparsed) values for the other components in this schema, keyed by path —
    /// the submitted form on a save, the entity's current values on a render.</summary>
    public IReadOnlyDictionary<string, string?> State { get; init; } = NoState;

    /// <summary>The raw value entered for another component, or null if it has none.</summary>
    public string? this[string path] => State.TryGetValue(path, out var value) ? value : null;

    /// <summary>The same context pointed at one particular record — used when a schema is
    /// rendered once per row.</summary>
    public EvaluationContext ForRecord(object? record) =>
        new() { Db = Db, Record = record, User = User, State = State };

    public DbContext RequireDb() => Db ?? throw new InvalidOperationException(
        "This component's configuration needs a DbContext, but none was supplied to the " +
        "evaluation context.");
}
