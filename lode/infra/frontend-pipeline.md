# Frontend Build Pipeline

## Commands
- **Dev**: `npm start` → `dotnet fable watch --cwd src/Client --run npx vite`
- **Build**: `npm run build` → `dotnet fable --cwd src/Client --run npx vite build`
- **Fable only**: `npm run build:client` → `dotnet fable --cwd src/Client`

## Architecture

```
src/Client/index.html              ← SPA entry
src/Client/vite.config.ts          ← Vite config + Tailwind plugin + API proxy
src/Client/App.fs                   ← F# React component + mount point
src/Client/App.fs.js                ← Fable output (generated, git-ignored)
src/Client/app.css                  ← @import "tailwindcss"; @plugin "daisyui"
src/Client/fable_modules/           ← Fable dependency compilation (git-ignored)
```

Fable runs with `--cwd src/Client`, so Vite's working directory is `src/Client/`. `index.html` references `./App.fs.js` (relative) which imports from `./fable_modules/`.

## Key Patterns

- Fable compiles `src/Client/BoxTracker.Client.fsproj` and its `Shared` reference in-place (`.fs.js` next to `.fs` files)
- React 18 `createRoot` API imported via Fable JS interop: `import "createRoot" "react-dom/client"`
- Browser/DOM code guarded with `#if FABLE_COMPILER` so `dotnet build` succeeds without Fable
- Tailwind CSS v4 with `@tailwindcss/vite` plugin (no PostCSS config needed)
- DaisyUI v5 via `@plugin "daisyui"` in `app.css`
- Vite dev server proxies `/api` to `http://localhost:5000` (Saturn backend)
- Client DTOs use `array` (not `list`) for collection fields — `JSON.parse` returns plain JS arrays, not Fable linked lists (`FSharpList` checks `tail == null` which is always true for JS arrays)
- Server must output PascalCase JSON (no `CamelCase` naming policy) — Fable record field names are preserved as-is in JS

## Production Build

`npx vite build` outputs to `dist/`:
- `dist/index.html` — entry
- `dist/assets/*.css` — Tailwind + DaisyUI (~31 KB gzip ~7 KB)
- `dist/assets/*.js` — React + Feliz + app (~151 KB gzip ~48 KB)
