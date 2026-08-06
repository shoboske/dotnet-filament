using System.Security.Claims;
using Demo.Data;
using Fila;
using Fila.Tooling;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(options =>
    options.UseSqlite("Data Source=demo.db"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser()));

builder.Services.AddFilaPanel(panel => panel
    .AtPath("admin")
    .Brand("Demo Admin")
    .UseDbContext<AppDb>()
    .RequireAuthorization("AdminOnly")
    // Demo-only credential check (admin/admin) — Fila owns the login page and routing; the
    // host app only says whether a username/password pair is valid and who they are. Real
    // apps wire this up to their own user store.
    .WithLogin((username, password, _) =>
    {
        var principal = username == "admin" && password == "admin"
            ? new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, username)], CookieAuthenticationDefaults.AuthenticationScheme))
            : null;

        return Task.FromResult(principal);
    })
    .DiscoverResources(typeof(Program).Assembly));

var app = builder.Build();

if (await app.RunFilaCommandsAsync(args)) return;

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
    DemoSeeder.SeedIfEmpty(db);
}

app.MapFilaPanel();

app.Run();
