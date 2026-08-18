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
