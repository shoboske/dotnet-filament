using System.Security.Claims;

using Fila.Panels;
using Demo.Data;
using Demo.Fila.Widgets;

namespace Demo.Fila;

public static class AdminPanel
{
    public static void Configure(PanelBuilder panel) => panel.AtPath("admin")
    .Brand("Demo Admin")
    .PrimaryColor("#f59e0b")
    .UseDbContext<AppDb>()
    .Widgets(typeof(DemoStatsWidget), typeof(RevenueChartWidget))
    .WithLogin((username, password, _) =>
    {
        var principal = username == "admin" && password == "admin"
            ? new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, username)], "Demo"))
            : null;

        return Task.FromResult(principal);
    })
    .DiscoverResources(typeof(Program).Assembly);
}
