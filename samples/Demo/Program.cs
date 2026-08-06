using System.Security.Claims;
using Demo;
using Demo.Data;
using Fila;
using Fila.Tooling;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(options =>
    options.UseSqlite("Data Source=demo.db"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser()));

builder.Services.AddFilaPanel(panel => panel
    .AtPath("admin")
    .Brand("Demo Admin")
    .UseDbContext<AppDb>()
    .RequireAuthorization("AdminOnly")
    .WithLogoutPath("/logout")
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

// Demo-only credential check (admin/admin) — real apps wire this up to their own user store.
app.MapGet("/login", (bool? error) => Results.Content(LoginPage.Render(error is true), "text/html"));

app.MapPost("/login", async (HttpContext ctx) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    if (username != "admin" || password != "admin")
        return Results.Redirect("/login?error=true");

    var identity = new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, username)], CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect("/admin");
});

app.MapPost("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapFilaPanel();

app.Run();
