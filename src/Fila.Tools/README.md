# Fila.Tools

`dotnet fila` — the command-line scaffolder for [Fila](https://github.com/shoboske/dotnet-filament).

A thin dispatcher, the same shape as `dotnet ef`: it finds your project and shells out to
`dotnet run -- fila <args>`, so the generators run inside your app against its real
`DbContext`. Your DI configuration is used, not re-implemented.

## Install

```bash
dotnet tool install --global Fila.Tools --prerelease
```

Or pinned per repository, which is what `samples/Demo` does:

```bash
dotnet new tool-manifest
dotnet tool install Fila.Tools --prerelease
```

## Commands

```
dotnet fila make:panel <Name>
dotnet fila make:resource <Entity> [--context <Name>] [--force]
dotnet fila make:page <Name> [--force]
dotnet fila make:widget <Name> [--force]
dotnet fila make:relation-manager <Parent> <Related> [--context <Name>] [--force]
dotnet fila make:action <Name> [--force]

  --project <path>   Target .csproj, when the current directory holds more than one.
  --help             Show this help.
  --version          Show the fila tool version.
```

The target app must reference [`Fila.Tooling`](https://www.nuget.org/packages/Fila.Tooling) (the
umbrella [`Fila`](https://www.nuget.org/packages/Fila) package includes it) and call
`await app.RunFilaCommandsAsync(args)` in `Program.cs` — that is what these commands dispatch
into.

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
