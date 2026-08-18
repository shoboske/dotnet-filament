using Fila.Support;

namespace Demo.Data;

/// <summary>Soft-deletable (implements ISoftDeletable) — a Customer's Orders reference it via a
/// required FK, so a hard delete would cascade-delete their whole order history. Soft delete
/// avoids that: see CustomerResource for the Restore/ForceDelete actions this unlocks.</summary>
public sealed class Customer : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset? DeletedAt { get; set; }

    public List<Order> Orders { get; set; } = [];
}
