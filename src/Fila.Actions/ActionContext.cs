using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fila.Actions;

/// <summary>Everything an <see cref="Action"/>'s Handle callback needs — the record it acts on
/// (already bound with whatever the action's schema submitted, for a schema-carrying action),
/// the request's DbContext, and the raw submitted values keyed by path. Mirrors what a
/// Filament action closure receives via its typed parameters ($record, $data, $livewire).</summary>
public sealed class ActionContext
{
    public required HttpContext HttpContext { get; init; }
    public required DbContext Db { get; init; }

    /// <summary>The entity this action acts on — a freshly-created blank instance for an action
    /// with <see cref="ActionRecordSource.New"/>, or the entity resolved by id otherwise. Never
    /// null by the time Handle runs.</summary>
    public object? Record { get; init; }

    /// <summary>Raw (unparsed) submitted values, keyed by field path — empty for an action with
    /// no schema. Already validated and bound onto <see cref="Record"/> by the time Handle
    /// runs; kept here too for a Handle that wants the raw strings back.</summary>
    public IReadOnlyDictionary<string, string?> FormData { get; init; } = EmptyState;

    public required CancellationToken Ct { get; init; }

    public ClaimsPrincipal? User => HttpContext.User;

    private static readonly IReadOnlyDictionary<string, string?> EmptyState = new Dictionary<string, string?>();
}
