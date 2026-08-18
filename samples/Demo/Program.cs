using System.Security.Claims;
using Demo.Data;
using Fila.Panels;
using Fila.Tooling;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(options =>
    options.UseSqlite("Data Source=demo.db"));

builder.Services.AddFilaPanel(panel => panel
    .AtPath("admin")
    .Brand("Demo Admin")
    .PrimaryColor("#f59e0b") // amber — demonstrates per-panel color customization
    .UseDbContext<AppDb>()
    // Demo-only credential check (admin/admin) — Fila owns the login page, routing, and (by
    // default) the authentication scheme itself; the host app only says whether a
    // username/password pair is valid and who they are. Real apps wire this up to their own
    // user store.
    .WithLogin((username, password, _) =>
    {
        var principal = username == "admin" && password == "admin"
            ? new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, username)], "Demo"))
            : null;

        return Task.FromResult(principal);
    })
    .DiscoverResources(typeof(Program).Assembly));

var app = builder.Build();

if (await app.RunFilaCommandsAsync(args)) return;

app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
    DemoSeeder.SeedIfEmpty(db);
}

app.MapFilaPanel();

app.Run();

// Makes this top-level-statement Program reachable as a generic type argument so
// WebApplicationFactory<Program> (tests/Fila.Tests) can boot this app in-process.
public partial class Program;
