<img src="assets/logo.svg" alt="" width="72" align="left" hspace="12" vspace="4">

# dotnet-filament

A dotnet package inspired by Laravel Filament.

<br clear="left">

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

The gaps that used to be visible here — no top bar, no numbered pagination, single-column forms,
`<select>`/numeric fields that didn't start blank, gray status badges, hand-drawn icons, inert
rows — were worked through in #29–#42 and are checked off in
[issue #27](https://github.com/shoboske/dotnet-filament/issues/27), which tracks this audit
screen by screen.

What's left in these shots is deliberate divergence rather than drift, and #27 records why:

- **Badge text.** Fila humanizes the enum (`Processing`) where the reference prints its raw
  backing value (`processing`).
- **Customers.** Demo's `CustomerResource` declares an Infolist, so it gains a View action the
  reference doesn't have; and it registers a relation manager, so its Edit opens a dedicated
  `/customers/{id}/edit` page (which hosts that manager) instead of the reference's modal. That
  second one also changes what clicking a row does: Filament prefers a record *URL* over a record
  *action*, so Demo's customer rows link to the edit page while its order rows — no relation
  manager, no URL — open the View modal, exactly as the same rule produces in reference.

The dashboard chart and stat sparklines match: both render through a locally-vendored Chart.js,
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

## Releasing to NuGet

`.github/workflows/release.yml` publishes every `src/` package — `Fila`, the ten packages it
umbrellas, `Fila.Testing`, and the `Fila.Tools` CLI — to nuget.org. It runs when a GitHub
Release is **published**, and takes the package version from that release's tag (`v0.2.0` and
`0.2.0` both mean `0.2.0`; a prerelease suffix like `v0.2.0-beta.1` is passed through as-is).

Before the first release, add a nuget.org API key scoped to the `Fila*` package IDs as the
`NUGET_API_KEY` repository secret (Settings → Secrets and variables → Actions). The workflow
fails with a clear message rather than a push error if it is missing.

To cut a release:

1. Bump `<Version>` in `Directory.Build.props` and the pinned `fila.tools` version in
   `samples/Demo/.config/dotnet-tools.json` — they are what local builds and `dotnet tool
   restore` use, and they should agree with what was last published. They are currently
   `0.0.1-alpha`.
2. Publish a GitHub Release tagged with that version.

The workflow builds the solution, runs the tests, packs at the tag's version, installs the
packed `Fila.Tools` and runs `fila --version` against it, uploads all the `.nupkg`/`.snupkg`
files as a workflow artifact, and only then pushes to nuget.org.

Each package's nuget.org page comes from the `README.md` next to its `.csproj` — `PackageIcon`
and `PackageReadmeFile` are set once in `Directory.Build.props`, and a new packable project that
forgets its README fails the pack with NU5039 rather than publishing a blank page.

`workflow_dispatch` runs the same thing on demand for a given version — with **publish**
unchecked it stops after the artifact upload, which is the way to inspect the packages without
releasing anything.

## The logo

`assets/logo.svg` is the source of the mark: an "F" whose arms are table rows, on the indigo a
Fila panel renders with before anyone calls `.PrimaryColor(...)`. NuGet only accepts a raster
icon, so every package ships `assets/icon.png` — a 128x128 rasterization of that SVG, wired up
by `<PackageIcon>` in `Directory.Build.props`. Regenerate it after any edit to the SVG:

```bash
python3 assets/render-icon.py   # needs Chromium; set CHROME=/path/to/chrome if it isn't on PATH
```

The panel UI is deliberately left alone: it renders to match `samples/FilamentReference` screen
for screen, and a brand mark in the topbar or on the login page would be a difference the
comparison above can't account for.
