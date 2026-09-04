# dotnet-filament

A dotnet package inspired by Laravel Filament.

Fila ports Filament's panel/table/form/action/widget conventions to .NET: Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens. `samples/Demo` is
a small two-resource (Customers, Orders) admin panel built on it; `samples/FilamentReference` is
the real PHP Laravel + Filament app it's ported from, kept in the repo as the ground truth for
what "correct" looks like.

## Screenshot comparison

Side by side, screen by screen: the **PHP** column is `samples/FilamentReference` (Laravel 12 +
Filament 5.7, real Livewire/Blade rendering); the **.NET** column is `samples/Demo` running on
Fila. Same seed data, same viewport, captured in the same session so differences are real, not
incidental.

| Screen | PHP — Filament (`samples/FilamentReference`) | .NET — Fila (`samples/Demo`) |
| --- | --- | --- |
| Login | ![Filament login](docs/screenshots/reference/login.png) | ![Fila login](docs/screenshots/demo/login.png) |
| Dashboard | ![Filament dashboard](docs/screenshots/reference/dashboard.png) | ![Fila dashboard](docs/screenshots/demo/dashboard.png) |
| Customers — list | ![Filament customers list](docs/screenshots/reference/customers-list.png) | ![Fila customers list](docs/screenshots/demo/customers-list.png) |
| Customers — create | ![Filament create customer modal](docs/screenshots/reference/customers-create.png) | ![Fila create customer modal](docs/screenshots/demo/customers-create.png) |
| Customers — edit | ![Filament edit customer modal](docs/screenshots/reference/customers-edit.png) | ![Fila edit customer page](docs/screenshots/demo/customers-edit.png) |
| Orders — list | ![Filament orders list](docs/screenshots/reference/orders-list.png) | ![Fila orders list](docs/screenshots/demo/orders-list.png) |
| Orders — create | ![Filament create order modal](docs/screenshots/reference/orders-create.png) | ![Fila create order modal](docs/screenshots/demo/orders-create.png) |
| Orders — view | ![Filament view order modal](docs/screenshots/reference/orders-view.png) | ![Fila view order modal](docs/screenshots/demo/orders-view.png) |

A few differences are visible on sight and tracked in
[issue #27](https://github.com/shoboske/dotnet-filament/issues/27): no top bar or numbered
pagination yet, form fields render single-column instead of Filament's responsive grid, the
`<select>`/numeric fields don't start blank, and status/row-action colors don't fully match. The
dashboard chart and stat sparklines do match — both render through a locally-vendored Chart.js,
not a hand-rolled substitute.

### Regenerating these screenshots

Both apps seed the same fixture data (3 customers, 42 orders) so a re-run stays comparable:

```bash
# PHP reference, from samples/FilamentReference
php artisan serve --host=127.0.0.1 --port=8001

# .NET port, from samples/Demo (mkdir -p artifacts/nuget-local first on a fresh checkout)
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5199 \
  dotnet run --no-launch-profile
```

Then drive both with Playwright (`PLAYWRIGHT_BROWSERS_PATH` pointed at a local Chromium — see
`CLAUDE.md`) at a 1440×900 viewport, logging in as `test@example.com` / `password` on the PHP app
and `admin` / `admin` on the .NET one.
