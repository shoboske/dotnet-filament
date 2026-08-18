using Microsoft.AspNetCore.Http;

namespace Fila.Tables;

/// <summary>
/// Table state (search/sort/page) rides entirely in the query string. This record is the
/// parsed form of it. Kept standalone (not folded into Table/Resource) so it can later become
/// one field on a phase-2 form snapshot rather than the whole story — see spec §10.
/// </summary>
public sealed record TableQuery(string? Search, string? Sort, string? Dir, int Page)
{
    public static TableQuery FromRequest(IQueryCollection query) => FromValues(key => query[key].ToString());

    /// <summary>Create/Edit/Delete submit as form posts, not query strings, but still carry the
    /// current search/sort/page along via hidden #fila-table-state inputs (hx-include) so the
    /// table re-renders in the same state it was in when the modal opened.</summary>
    public static TableQuery FromForm(IFormCollection form) => FromValues(key => form[key].ToString());

    private static TableQuery FromValues(Func<string, string?> get)
    {
        var search = get("search");
        var sort = get("sort");
        var dir = get("dir");
        var pageRaw = get("page");

        var page = int.TryParse(pageRaw, out var parsed) && parsed > 0 ? parsed : 1;

        return new TableQuery(
            string.IsNullOrWhiteSpace(search) ? null : search,
            string.IsNullOrWhiteSpace(sort) ? null : sort,
            string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc",
            page);
    }

    /// <summary>hx-vals payload for a sortable header: toggles direction if it's already the
    /// active sort column, otherwise starts ascending; always resets to page 1.</summary>
    public string SortValsFor(ITableColumn column)
    {
        var nextDir = Sort == column.Path && Dir == "asc" ? "desc" : "asc";
        return $$"""{"sort": "{{column.Path}}", "dir": "{{nextDir}}", "page": "1"}""";
    }
}

public sealed class PagedRows
{
    public required IReadOnlyList<object> Rows { get; init; }
    public required int Page { get; init; }
    public required int PerPage { get; init; }
    public required int Total { get; init; }

    public int LastPage => Total == 0 ? 1 : (int)Math.Ceiling(Total / (double)PerPage);
}
