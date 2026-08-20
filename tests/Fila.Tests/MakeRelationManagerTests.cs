using Demo.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Fila.Tooling.FileGenerators;
using Xunit;

namespace Fila.Tests;

/// <summary>Phase 9 acceptance: `make:relation-manager`'s generated source actually compiles,
/// against the same Customer/Order relationship samples/Demo's own hand-written
/// OrdersRelationManager already uses — proving the FK/column inference produces a real,
/// working scope, not just syntactically valid nonsense.</summary>
public sealed class MakeRelationManagerTests
{
    [Fact]
    public void Generate_ProducesARelationManagerScopedByTheRealForeignKey()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDb>().UseSqlite(connection).Options;
        using var db = new AppDb(options);

        var customerType = db.Model.FindEntityType(typeof(Customer))!;
        var orderType = db.Model.FindEntityType(typeof(Order))!;

        var result = RelationManagerClassGenerator.Generate(customerType, orderType, "Demo");

        Assert.NotNull(result);
        Assert.Contains("class OrderRelationManager : RelationManager<Customer, Order>", result!.Source);
        Assert.Contains("o.CustomerId == parent.Id", result.Source);
        Assert.True(result.ColumnCount > 0);
        GeneratedSourceCompiler.AssertCompiles(result.Source);
    }

    [Fact]
    public void Generate_ReturnsNullWhenThereIsNoForeignKeyBackToTheParent()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDb>().UseSqlite(connection).Options;
        using var db = new AppDb(options);

        var customerType = db.Model.FindEntityType(typeof(Customer))!;
        var orderType = db.Model.FindEntityType(typeof(Order))!;

        // Backwards — Customer has no FK back to Order, only the reverse.
        var result = RelationManagerClassGenerator.Generate(orderType, customerType, "Demo");

        Assert.Null(result);
    }
}
