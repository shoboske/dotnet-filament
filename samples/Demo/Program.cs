using System.Security.Claims;
using Demo.Data;
using Demo.Fila;
using Fila.Panels;
using Fila.Tooling;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(options =>
    options.UseSqlite("Data Source=demo.db"));

builder.Services.AddFilaPanel(AdminPanel.Configure);
builder.Services.AddFilaPanel(StaffPanel.Configure);

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
