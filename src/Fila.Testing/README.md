# Fila.Testing

Assertion helpers for testing a Fila panel.

Fila answers actions with server-rendered HTML plus an `HX-Trigger` header, so asserting that an
action notified the user or closed its modal otherwise means hand-parsing that header's JSON in
every test. These do it for you.

Deliberately tied to no test framework: they throw `FilaAssertionException`, and an unhandled
exception fails a test under xUnit, NUnit, MSTest or anything else.

## Install

```bash
dotnet add package Fila.Testing --prerelease
```

Fila is at `0.0.1-alpha`, so `--prerelease` is required until a stable version ships.

## Assertions

```csharp
var response = await client.PostAsync("/admin/orders/1/mark-shipped", null);

response.AssertNotificationTriggered(title: "Marked as shipped", color: "success");
response.AssertModalClosed();
```

Both are extension methods on `HttpResponseMessage`, so they drop straight into a
`WebApplicationFactory` test.

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
