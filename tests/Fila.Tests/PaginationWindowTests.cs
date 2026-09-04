using Fila.Tables;
using Xunit;

namespace Fila.Tests;

/// <summary>Unit coverage for PaginationWindow.Build's port of Laravel's UrlWindow — the branches
/// (small slider, too-close-to-beginning, too-close-to-ending, full slider with two ellipses)
/// nothing in samples/Demo's seeded data (max 2 pages) ever exercises end to end, since none of
/// its resources have 8+ pages. See OrdersListTests for the end-to-end 2-page case this doesn't
/// cover.</summary>
public sealed class PaginationWindowTests
{
    private static string Render(IReadOnlyList<PaginationItem> items) =>
        string.Join(' ', items.Select(i => i.IsEllipsis ? "…" : i.Page.ToString()));

    [Fact]
    public void SinglePage_HasNoItems()
    {
        Assert.Empty(PaginationWindow.Build(currentPage: 1, lastPage: 1));
    }

    [Fact]
    public void FewPages_ShowsEveryPage_NoEllipsis()
    {
        // lastPage < 8 (Filament's onEachSide(0) small-slider threshold) -- the whole range.
        Assert.Equal("1 2 3 4 5 6 7", Render(PaginationWindow.Build(currentPage: 4, lastPage: 7)));
    }

    [Fact]
    public void ManyPages_CurrentNearTheStart_ShowsLeadingRunThenTheLastTwoPages()
    {
        Assert.Equal("1 2 3 4 … 19 20", Render(PaginationWindow.Build(currentPage: 1, lastPage: 20)));
        Assert.Equal("1 2 3 4 … 19 20", Render(PaginationWindow.Build(currentPage: 4, lastPage: 20)));
    }

    [Fact]
    public void ManyPages_CurrentNearTheEnd_ShowsTheFirstTwoPagesThenATrailingRun()
    {
        Assert.Equal("1 2 … 17 18 19 20", Render(PaginationWindow.Build(currentPage: 20, lastPage: 20)));
        Assert.Equal("1 2 … 17 18 19 20", Render(PaginationWindow.Build(currentPage: 17, lastPage: 20)));
    }

    [Fact]
    public void ManyPages_CurrentInTheMiddle_ShowsBothEdgesAndTheCurrentPageAlone()
    {
        // onEachSide(0) -- the slider around the current page is just the current page itself.
        Assert.Equal("1 2 … 10 … 19 20", Render(PaginationWindow.Build(currentPage: 10, lastPage: 20)));
    }
}
