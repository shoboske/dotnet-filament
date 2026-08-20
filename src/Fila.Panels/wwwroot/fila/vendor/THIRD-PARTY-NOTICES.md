# Vendored third-party scripts

Fila has zero runtime dependencies fetched from a CDN — everything the panel UI needs client-side
ships as a static asset from this folder instead of `unpkg.com`/`cdn.jsdelivr.net`/etc, so the
admin UI works the same whether or not the deployment environment can reach those hosts. Each
file below is the vendor's own unmodified minified build, copied from the npm package version
noted. Bump a version by re-running the same `npm pack <name>@<version>` + copy the relevant
`dist/*` file — do not hand-edit these.

| File | Package | Version | License |
| --- | --- | --- | --- |
| `htmx.min.js` | [htmx.org](https://www.npmjs.com/package/htmx.org) | 2.0.4 | 0BSD |
| `alpine.min.js` | [alpinejs](https://www.npmjs.com/package/alpinejs) (`dist/cdn.min.js` — the IIFE build meant for a plain `<script>` tag, auto-starts Alpine) | 3.14.1 | MIT |
| `chart.min.js` | [chart.js](https://www.npmjs.com/package/chart.js) (`dist/chart.umd.min.js` — the UMD build, exposes a global `Chart`) | 4.5.1 | MIT |
