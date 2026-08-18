using System.Data.Common;
using Demo.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fila.Tests;

/// <summary>Boots samples/Demo in-process against an isolated SQLite connection instead of the
/// developer's real demo.db, so tests never touch (or depend on) local dev state. One instance
/// is shared per test class via IClassFixture&lt;DemoAppFactory&gt; — different test classes get
/// fully separate databases, tests within a class share one seeded database.</summary>
public class DemoAppFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDb>>();

            // Kept open for the factory's lifetime — an in-memory SQLite database is dropped the
            // moment its one and only connection closes, so this connection *is* the database.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<AppDb>(options => options.UseSqlite(_connection));

            ConfigureTestServices(services);
        });
    }

    /// <summary>Hook for a test that needs extra services in the demo app — a second panel
    /// carrying test-only resources, for instance. Runs after the database is swapped, so a
    /// panel registered here still gets mapped by the app's own MapFilaPanel call.</summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing) _connection?.Dispose();
    }
}
