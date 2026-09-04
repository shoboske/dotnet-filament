using Fila.Tables;

namespace Fila.Panels.Rendering;

/// <summary>What _PaginationNav.cshtml needs to render one numbered pagination control — shared
/// between the main resource list (_Pagination.cshtml) and a relation manager's nested table
/// (_RelationManagerTable.cshtml), which differ only in which URL/htmx target/state block a
/// page click re-requests against.</summary>
public sealed record PaginationNavModel(PagedRows Paged, string Url, string HxTarget, string HxInclude, bool PushUrl);
