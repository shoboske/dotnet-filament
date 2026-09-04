namespace Fila.Tables;

/// <summary>One item in a numbered pagination list: either a real page number, or a "…" gap
/// between two page ranges.</summary>
public readonly record struct PaginationItem(int Page, bool IsEllipsis)
{
    public static PaginationItem Of(int page) => new(page, false);

    public static readonly PaginationItem Ellipsis = new(0, true);
}

/// <summary>Builds the page-number list a numbered pagination control shows — a port of
/// Laravel's <c>Illuminate\Pagination\UrlWindow</c> + <c>LengthAwarePaginator::elements()</c>,
/// with Filament's own <c>onEachSide(0)</c> (Tables\Concerns\CanPaginateRecords::getPageOptions)
/// baked in rather than left configurable, since nothing in Fila varies it. Laravel's own
/// algorithm branches on whether the current page is near the start, near the end, or has room
/// on both sides — ported branch for branch rather than approximated, so the exact page someone
/// lands on gets the exact window Filament would show for it.</summary>
public static class PaginationWindow
{
    public static IReadOnlyList<PaginationItem> Build(int currentPage, int lastPage)
    {
        if (lastPage <= 1) return [];

        // UrlWindow::get(): lastPage < (onEachSide * 2) + 8, onEachSide = 0.
        if (lastPage < 8) return Range(1, lastPage).ToList();

        // UrlWindow::getUrlSlider(): $window = onEachSide + 4 = 4.
        const int window = 4;

        if (currentPage <= window)
        {
            // getSliderTooCloseToBeginning(): first = 1..(window+onEachSide), last = finish.
            return
            [
                .. Range(1, window),
                PaginationItem.Ellipsis,
                .. Range(lastPage - 1, lastPage),
            ];
        }

        if (currentPage > lastPage - window)
        {
            // getSliderTooCloseToEnding(): first = start, last = (lastPage-(window+onEachSide-1))..lastPage.
            return
            [
                .. Range(1, 2),
                PaginationItem.Ellipsis,
                .. Range(lastPage - 3, lastPage),
            ];
        }

        // getFullSlider(): first = start, slider = current±onEachSide (just current, since
        // onEachSide is 0), last = finish.
        return
        [
            .. Range(1, 2),
            PaginationItem.Ellipsis,
            PaginationItem.Of(currentPage),
            PaginationItem.Ellipsis,
            .. Range(lastPage - 1, lastPage),
        ];
    }

    private static IEnumerable<PaginationItem> Range(int from, int to)
    {
        for (var page = from; page <= to; page++) yield return PaginationItem.Of(page);
    }
}
