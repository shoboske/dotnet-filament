namespace Fila.Tables;

public enum ColumnKind
{
    Text,
    Badge,
    Money,
    Date,
    Bool,
}

public enum ColumnAlign
{
    Start,
    Center,
    End,
}

/// <summary>
/// Non-generic view of a column, used by rendering and query code that doesn't know TEntity.
/// </summary>
public interface ITableColumn
{
    string Path { get; }
    string Label { get; }
    ColumnKind Kind { get; }
    ColumnAlign Alignment { get; }
    bool IsSearchable { get; }
    bool IsSortable { get; }

    object? GetValue(object entity);
    string FormatDisplay(object entity);
    string? GetBadgeColor(object entity);
}

public sealed class TableColumn<T> : ITableColumn
{
    private readonly Func<T, object?> _accessor;
    private string _label;
    private ColumnAlign _alignment = ColumnAlign.Start;
    private Func<object?, string>? _colorSelector;

    internal TableColumn(ColumnKind kind, string path, Func<T, object?> accessor)
    {
        Kind = kind;
        Path = path;
        _accessor = accessor;
        _label = Humanize(path);
    }

    public string Path { get; }
    public ColumnKind Kind { get; }
    public bool IsSearchable { get; private set; }
    public bool IsSortable { get; private set; }

    string ITableColumn.Label => _label;
    ColumnAlign ITableColumn.Alignment => _alignment;

    public TableColumn<T> Label(string label)
    {
        _label = label;
        return this;
    }

    public TableColumn<T> Searchable(bool value = true)
    {
        IsSearchable = value;
        return this;
    }

    public TableColumn<T> Sortable(bool value = true)
    {
        IsSortable = value;
        return this;
    }

    public TableColumn<T> Alignment(ColumnAlign alignment)
    {
        _alignment = alignment;
        return this;
    }

    /// <summary>Only meaningful on Badge columns: maps the raw value to a tone name
    /// (neutral | info | success | warning | danger).</summary>
    public TableColumn<T> Colors<TValue>(Func<TValue, string> selector)
    {
        _colorSelector = raw => raw is TValue typed ? selector(typed) : "neutral";
        return this;
    }

    object? ITableColumn.GetValue(object entity) => _accessor((T)entity);

    string ITableColumn.FormatDisplay(object entity)
    {
        var value = _accessor((T)entity);
        return Kind switch
        {
            ColumnKind.Money => value is null ? string.Empty : FormatMoney(value),
            ColumnKind.Date => FormatDate(value),
            ColumnKind.Bool => value is true ? "Yes" : "No",
            _ => value?.ToString() ?? string.Empty,
        };
    }

    string? ITableColumn.GetBadgeColor(object entity)
    {
        if (_colorSelector is null) return null;
        return _colorSelector(_accessor((T)entity));
    }

    // Fixed to en-US rather than the server's current culture — money columns should look
    // the same regardless of what locale the container happens to boot with.
    private static readonly System.Globalization.CultureInfo DisplayCulture =
        System.Globalization.CultureInfo.GetCultureInfo("en-US");

    private static string FormatMoney(object value) => value switch
    {
        decimal d => d.ToString("C", DisplayCulture),
        double d => d.ToString("C", DisplayCulture),
        float f => f.ToString("C", DisplayCulture),
        _ => Convert.ToDecimal(value).ToString("C", DisplayCulture),
    };

    private static string FormatDate(object? value) => value switch
    {
        DateTime dt => dt.ToString("MMM d, yyyy", DisplayCulture),
        DateTimeOffset dto => dto.ToString("MMM d, yyyy", DisplayCulture),
        null => string.Empty,
        _ => value.ToString() ?? string.Empty,
    };

    private static string Humanize(string path)
    {
        var name = path.Contains('.') ? path[(path.LastIndexOf('.') + 1)..] : path;
        var chars = new List<char>();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                chars.Add(' ');
            chars.Add(name[i]);
        }
        return new string(chars.ToArray());
    }
}
