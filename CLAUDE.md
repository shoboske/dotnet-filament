# Notes for agents

## Setup
- No .NET SDK preinstalled, and `dotnet-install.sh` is blocked by the network policy.
  `apt-get install dotnet-sdk-10.0` off `packages.microsoft.com` works.
- `nuget.config` lists `artifacts/nuget-local`, missing on a fresh checkout — NuGet then
  fails *every* restore with NU1301. `mkdir -p artifacts/nuget-local` first.

## Running samples/Demo
- Static web assets are served only in Development. Under the default Production
  environment `/_content/Fila/fila/fila.css` 404s — that is not a regression.
- `StaticWebAssetBasePath` in `Fila.Panels.csproj` overrides the whole base path,
  `_content/` prefix included. A bare `Fila` serves from `/Fila/...`. `StaticAssetTests`
  guards this.

## The fila CLI
- `dotnet fila` resolves only from `samples/Demo` or below — the tool manifest lives in
  `samples/Demo/.config/`.
- After `dotnet pack src/Fila.Tools`, the tool still runs the *cached* package unless you
  `rm -rf ~/.nuget/packages/fila.tools` or bump the version. Silently stale otherwise.
- `make:resource X --force` overwrites hand-edited demo resources. `git checkout` after.

## Porting from Filament
- The upstream reference is `git clone --branch 6.x https://github.com/filamentphp/filament`.
  Clone it *before* writing a port, not after — an issue saying "if useful" still means clone
  it. Check out the branch the issue names and confirm with `git rev-parse --abbrev-ref HEAD`;
  don't substitute another branch or add a fallback chain (`4.x || 3.x` silently succeeds and
  you never notice).
- **Every implementation detail is a lookup, not a recall.** Anything with a right answer
  upstream — a class name, a padding value, a default, a method's return type, which of two
  elements nests inside the other — gets read out of the source before it gets written. Working
  from memory of "how Filament does it" reliably produces something that looks plausible and is
  wrong in a dozen small ways: invented `fi-*` names, a 12-column grid where Filament has 2,
  `columnSpan`/`getSort()` defaults off by one, an icon in the wrong row, a chart with no
  aspect ratio, `Stat::chart()` missing entirely. Each of those compiled and rendered fine.
- Sources, in decreasing order of authority. Go up a level whenever the lower one is silent:
  1. **Rendered HTML from a real Filament app.** Settles nesting and which classes actually
     ship — it showed `.fi-grid-col > .fi-sc-component > widget` and the description carrying
     `fi-text-color-700 dark:fi-text-color-400`.
  2. **`packages/panels/dist/theme.css`** (compiled). The *only* place plugin-generated rules
     are written out — `.fi-grid-col{grid-column:var(--col-span-default)}` and the whole
     `--cols-*`/`--col-span-*` mechanism appear nowhere in the source CSS.
  3. `packages/*/resources/css/*.css` and the blade views.
  4. The PHP classes, for defaults and API shape.
- Check for `@deprecated` before copying a component. `<x-filament-widgets::widgets>` and its
  `.fi-wi` grid are the *old* path; `Pages\Dashboard` builds a schema `Grid` instead, and
  `.fi-wi-widget` is not in the compiled theme at all.
- PHP type idioms often map to something stronger in C#, and should. `Resource::getWidgets()`
  returns `array<class-string<Widget>>` — a constraint only static analysis enforces. The port
  is a generic-constrained descriptor (`WidgetRegistration.Of<TWidget>() where TWidget : Widget`),
  which turns a boot-time throw into a compile error. Port the *intent*, not the PHP's limits.
- Class names and spacing are not up for invention. `fi-*` names come from Filament's own
  `packages/*/resources/css/*.css`, and Fila hardcodes the resolved Tailwind values with the
  utility named in a comment. `text-gray-500 dark:text-gray-400` is exactly `--fi-text-secondary`
  and `text-gray-400 dark:text-gray-500` is exactly `--fi-text-muted` — reuse the token, don't
  re-pick a hex.
- Where a Fila token is *not* equivalent, use the literal Tailwind value and say why in a
  comment. Fila's badge foregrounds are 700/**300**; Filament's stat description is 700/**400**
  and its chart fills are `color-50` / `color-400/10` — a 10% wash, not the solid dark shade
  `--fi-accent-soft-bg` gives you.
- Widget/section chrome lives in `fila.widgets.css`, imported from `fila.css` the way
  `fila.actions.css` is. `.fi-section` belongs to Filament's *support* package; it sits in
  fila.widgets.css only until a second Fila package needs it.

## Editing
- `FilaCli.cs` and `Scaffolder.cs` hold code-generation templates as string literals.
  Sweeping renames rewrite them too — they still compile and tests still pass, but the
  generated code is wrong. Run the generator and compile its output.
- Razor views compile into their own assembly, so `internal` types they use break when
  views move projects. Prefer `InternalsVisibleTo` over widening to public.
- Two DSL overloads differing only in a lambda's parameter type (`Func<DbContext,_>` vs
  `Func<EvaluationContext,_>`) make every `x => ...` call site CS0121. Use a second name.

## Verifying "no behavior change"
- `git worktree add <dir> main`, run both revisions on different ports, drive both through
  the same requests, diff byte for byte. Reset both databases first; ignore `Date`,
  `ETag`, `Last-Modified`.
- `dotnet run` ignores `ASPNETCORE_URLS` — `launchSettings.json` pins port 5177, so the
  second app fails to bind. Pass `--no-launch-profile`.
- CI is Azure Pipelines: it reports as a GitHub *check run*. `get_status` shows an empty
  pending result and looks like no CI exists — use `get_check_runs`.
