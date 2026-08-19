using System.Security.Claims;
using Demo.Data;
using Demo.Fila.Widgets;
using Fila.Widgets;
using Fila.Panels;

namespace Demo.Fila;

public static class StaffPanel
{
    public static void Configure(PanelBuilder panel) => panel
        .AtPath("staff")
        .Brand("Staff")
        .UseDbContext<AppDb>()
        .Widgets(WidgetRegistration.Of<DemoStatsWidget>(), WidgetRegistration.Of<RevenueChartWidget>())
        .PrimaryColor("#ef4444")
        .WithLogin((username, password, _) =>
        {
            var principal = username == "staff" && password == "staff"
                ? new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, username)], "Demo"))
                : null;

            return Task.FromResult(principal);
        })
        .DiscoverResources(typeof(Program).Assembly);
}
