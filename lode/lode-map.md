# Lode Map

## Root Files
- [summary.md](summary.md) — one-paragraph project snapshot
- [terminology.md](terminology.md) — domain language glossary
- [practices.md](practices.md) — coding standards and patterns

## Domain
- [domain/types.md](domain/types.md) — core domain types, smart constructors, Container DU, aggregate roots
- [domain/persistence.md](domain/persistence.md) — SQLite schema, move-derived placement, FTS5 search, Storage API
- [domain/api.md](domain/api.md) — server API handlers, DTOs, request/response contracts, label endpoints
- [domain/image-optimization.md](domain/image-optimization.md) — photo compression, thumbnail generation, WebP serving
- [domain/async-photo-processing.md](domain/async-photo-processing.md) — durable photo_job queue, background worker, upload/processing split

## Infrastructure
- [infra/stack.md](infra/stack.md) — technology choices and versions
- [infra/frontend-pipeline.md](infra/frontend-pipeline.md) — Fable + Vite + Tailwind/DaisyUI build pipeline
- [infra/deployment.md](infra/deployment.md) — Oracle Cloud VM setup, nginx config, systemd service, GitHub Actions CI/CD, iptables rules

## UI
- [ui/design-system.md](ui/design-system.md) — spacing scale, typography hierarchy, button sizing standards, form styling, layout patterns
- [ui/pages.md](ui/pages.md) — client-side pages, routing, navigation structure
- [ui/mobile-responsive-design.md](ui/mobile-responsive-design.md) — responsive design improvements, mobile-first layout, touch-friendly UX

## Plans
- (none yet)

## Tmp
- session scraps (git-ignored)

## Lessons Learned
- Fable 5.0.0 requires .NET 10 SDK; `Fable.Template` NuGet package is stale at 3.9.0 — scaffold manually
- When modules and types share the same name (e.g. `module LocationCode` / `type LocationCode`), use fully qualified paths (`BoxTracker.LocationCode.create`) in external code to avoid ambiguity
- Saturn 0.17.0 pulls in vulnerable JWT packages (NU1902 warnings) — cosmetic for a local-only app but noted
- `Feliz.UseListener` 0.3.0 and `Feliz.Router` 3.0.0 are incompatible with Fable 5 (`useCallbackRef` removed, type mismatches) — removed; routing to be reimplemented later
- .NET 10 SDK creates `dotnet-tools.json` at repo root (not `.config/`); fails to install Fable on .NET 8 SDK
- Feliz 3.3.3 does not expose `ReactDOM.createRoot` or `ReactDOM.render` — use `import "createRoot" "react-dom/client"` with Fable JS interop
- All browser/DOM code in Client must be guarded with `#if FABLE_COMPILER` so `dotnet build` succeeds
- `[<Emit>]` function bodies must use `failwith "JS only"` — `importValue`/`jsNative` are not available in `dotnet build`; the body is never called under Fable
- Feliz `prop.onChange` requires typed lambda parameters (e.g. `fun (s: string) ->`) to resolve overload ambiguity
- F# anonymous record types inside generic args (e.g. `ofJson<{| error: string |}>`) fail to parse — use named types instead
- `Cmd.ofEffect` takes `Dispatch<'msg> -> unit`, not `'msg list` — to dispatch synchronously: `Cmd.ofEffect (fun d -> d msg)`
- Giraffe 8.x uses `BindJsonAsync<'T>()` (not `BindJson`); `ctx.Request.ReadFormAsync()` for multipart form data
- Fable's `list` is a linked list with `head`/`tail` fields — `JSON.parse` returns plain JS arrays which have no `tail`, so `List.isEmpty` always returns `true`. Use `array` for all DTO collection fields consumed from JSON
- Server JSON must use PascalCase (no `CamelCase` naming policy) — Fable record fields compile to JS with their exact F# names, and raw `JSON.parse` does not remap property casing
- `SyncItemToSearch` must NOT be called from both `AddItem` and `RecordMove` — `RecordMove` already syncs FTS5; double-calling creates duplicate entries
- SQLite `ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC)` is the standard pattern for deriving current placement from the move log
- Old `data/boxtracker.db` is incompatible after schema changes — must be deleted before running
