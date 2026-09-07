# Fila.Tooling

Fila's in-app CLI — the `make:*` scaffolding that generates resources, pages, widgets, actions
and relation managers against your project's own `DbContext`.

It runs *inside* your app rather than outside it: `RunFilaCommandsAsync` intercepts the process
before the web host starts serving, so a generator reads the real, fully-configured `DbContext`
the app would serve a request with. Nothing about your DI setup has to be duplicated or guessed.

## Install

```bash
dotnet add package Fila.Tooling --prerelease
```

Most apps take the umbrella [`Fila`](https://www.nuget.org/packages/Fila) package instead —
it pulls in every Fila package at once. Fila is at `0.0.1-alpha`, so `--prerelease` is required
until a stable version ships.

## Wire it up

```csharp
var app = builder.Build();

if (await app.RunFilaCommandsAsync(args)) return;   // handled a fila command, don't start serving

app.MapFilaPanel();
app.Run();
```

Then either invoke it directly:

```bash
dotnet run -- fila make:resource Order
```

...or install [`Fila.Tools`](https://www.nuget.org/packages/Fila.Tools) for the friendlier
`dotnet fila make:resource Order`, which shells out to exactly the command above.

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
