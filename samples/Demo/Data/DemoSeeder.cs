namespace Demo.Data;

public static class DemoSeeder
{
    public static void SeedIfEmpty(AppDb db)
    {
        if (db.Customers.Any()) return;

        var acme = new Customer { Name = "Acme Corp", Email = "buyer@acme.test" };
        var globex = new Customer { Name = "Globex", Email = "ops@globex.test" };
        var initech = new Customer { Name = "Initech", Email = "ap@initech.test" };

        db.Customers.AddRange(acme, globex, initech);

        var statuses = new[]
        {
            OrderStatus.Pending, OrderStatus.Processing, OrderStatus.Shipped,
            OrderStatus.Delivered, OrderStatus.Cancelled,
        };
        var customers = new[] { acme, globex, initech };

        var orders = Enumerable.Range(1, 42).Select(i => new Order
        {
            Reference = $"ORD-{1000 + i}",
            Customer = customers[i % customers.Length],
            Status = statuses[i % statuses.Length],
            Total = 19.99m + i * 7.25m,
            CreatedAt = DateTime.UtcNow.AddDays(-i),
        });

        db.Orders.AddRange(orders);
        db.SaveChanges();
    }
}
