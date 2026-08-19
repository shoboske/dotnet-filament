using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fila.Actions;

/// <summary>What a <see cref="BulkAction"/>'s Handle callback needs — the DbContext and the set
/// of records the request selected, resolved by id from the checked table rows.</summary>
public sealed class BulkActionContext
{
    public required HttpContext HttpContext { get; init; }
    public required DbContext Db { get; init; }
    public required IReadOnlyList<object> Records { get; init; }
    public required CancellationToken Ct { get; init; }

    public ClaimsPrincipal? User => HttpContext.User;
}
