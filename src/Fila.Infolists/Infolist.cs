using System.Linq.Expressions;

namespace Fila.Infolists;

/// <summary>Non-generic view of an infolist, used by rendering code that doesn't know
/// TEntity.</summary>
public interface IInfolist
{
    IReadOnlyList<IEntry> Entries { get; }
}

/// <summary>A read-only counterpart to Fila.Forms.Form&lt;T&gt; — same shape, same builder
/// style, but entries instead of fields and no submission. Kept as its own sibling type built
/// on Fila.Schemas rather than unified with Form: Fila's Action framework mounts a form via a
/// schema factory it validates and binds on submit, which an infolist never does, so folding
/// the two together would mean threading a "this one never submits" branch through code that
/// currently doesn't need to know the difference.</summary>
public sealed class Infolist<T> : IInfolist
{
    private IReadOnlyList<IEntry> _entries = [];

    IReadOnlyList<IEntry> IInfolist.Entries => _entries;

    /// <summary>Takes the base type, so an entry type declared outside Fila drops into the
    /// same call: .Entries(new RatingEntry&lt;Product&gt;(p =&gt; p.Stars)).</summary>
    public Infolist<T> Entries(params Entry<T>[] entries)
    {
        _entries = entries;
        return this;
    }

    public TextEntry<T> Text(Expression<Func<T, object?>> selector) => new(selector);

    public TextEntry<T> Money(Expression<Func<T, object?>> selector) => new TextEntry<T>(selector).Money();

    public TextEntry<T> Badge(Expression<Func<T, object?>> selector) => new TextEntry<T>(selector).Badge();

    public TextEntry<T> Date(Expression<Func<T, object?>> selector) => new TextEntry<T>(selector).Date();

    public TextEntry<T> Bool(Expression<Func<T, object?>> selector) => new TextEntry<T>(selector).Boolean();
}
