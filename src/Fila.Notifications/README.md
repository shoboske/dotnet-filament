# Fila.Notifications

Fila's notifications: the flash message an action raises when it succeeds or fails.

Delivered to the browser on htmx's `HX-Trigger` response header rather than held in session
state, so the notification arrives with the response that caused it. Ported from Filament's
notifications package.

## Install

```bash
dotnet add package Fila.Notifications --prerelease
```

Most apps take the umbrella [`Fila`](https://www.nuget.org/packages/Fila) package instead —
it pulls in every Fila package at once. Fila is at `0.0.1-alpha`, so `--prerelease` is required
until a stable version ships.

## Raise one from an action

```csharp
.Notify(Notification.Make().Title("Marked as shipped").Success())
```

`Success()`, `Danger()`, `Warning()` and `Info()` are shorthands over `Color(...)`, which takes
the same color names the rest of Fila uses.

Implement `IFilaNotificationStore` to put notifications somewhere other than the response header.

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
