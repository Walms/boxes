# AGENTS.md

## Environment Setup — CRITICAL

**You MUST run all commands inside the nix development shell.** This project requires .NET 10 SDK which is only available via the flake.

### Running commands as an agent (non-interactive shells)

**IMPORTANT: The agent (opencode) cannot inherit your direnv environment.** Every bash command MUST be wrapped with `nix develop --command`:

```bash
nix develop --command dotnet build src/Client/BoxTracker.Client.fsproj
nix develop --command dotnet test tests/Domain.Tests/
nix develop --command npm run build
```

You can chain commands in a single nix shell invocation:

```bash
nix develop --command bash -c "dotnet tool restore && dotnet build src/Server/BoxTracker.Server.fsproj"
```

**DO NOT run `dotnet` or `npm` commands directly** — they will use the system .NET 8 SDK and fail with:
> error NETSDK1045: The current .NET SDK does not support targeting .NET 10.0

### If direnv is not set up (for interactive use)

Create `.envrc` at repo root with:

```
use flake
```

Then run `direnv allow`. This lets humans auto-enter the shell when they `cd` into the repo.

## Build & Test Commands

These must all be run inside the nix shell:

```bash
dotnet build src/Client/BoxTracker.Client.fsproj   # Client (netstandard2.0)
dotnet build src/Server/BoxTracker.Server.fsproj    # Server (net10.0)
dotnet test tests/Domain.Tests/                     # Domain unit + property tests
dotnet test tests/Api.Tests/                        # Integration tests (temp SQLite)
npm run build:client                                # Fable compile only
npm run build                                       # Fable + Vite production build
```

## Runtime

To run the app locally:

```bash
mkdir -p data                          # only needed first time
BOXTRACKER_DATA=$PWD/data nix develop --command bash -c \
  "dotnet run --project src/Server/BoxTracker.Server.fsproj --urls http://localhost:5000" &
nix develop --command bash -c "npm start" &
# App at http://localhost:5173, API at http://localhost:5000
```

- Server stores SQLite DB and photos in `BOXTRACKER_DATA` (defaults to `./data`)
- Vite dev server proxies `/api/*` to ASP.NET backend on port 5000
- `vite.config.ts` lives in `src/Client/` (Vite's cwd is `src/Client/` via Fable's `--cwd`)

## Data Model — Event-Sourced Moves

Items, boxes, and locations exist as independent entities. Placement is derived from an append-only `move` table — there are no FK columns for `item.box_id` or `box.location_code`.

### Schema

| Table | Columns |
|-------|---------|
| `location` | `code` (PK), `name`, `is_archived`, `created_at` |
| `box` | `id` (PK), `label`, `created_at` |
| `item` | `id` (PK), `name`, `photo_path`, `added_at` |
| `move` | `id` (PK), `entity_type`, `entity_id`, `to_type`, `to_id`, `moved_at` |

### Container DU (`src/Shared/Domain/Container.fs`)

```fsharp
type Container =
    | Unassigned
    | InBox of BoxId
    | AtLocation of LocationCode
```

Both `Item.Placement` and `Box.Placement` are `Container` values derived from the latest move for that entity. The domain types in `Types.fs` carry `Placement` fields populated by the storage layer.

### How placement is derived

A `move` row records: `entity_type` (`"item"` or `"box"`), `entity_id`, `to_type` (`"box"`, `"location"`, or NULL for unassigned), `to_id`, and `moved_at`. The current placement for any entity is the most recent move (by `moved_at DESC`). Storage queries use `ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC)` to join with the move log.

### Key behaviours

- Creating a box → no move (starts `Unassigned`)
- `AddItem(boxId, ...)` → creates item + move (`InBox boxId`)
- `CreateItem(name)` → creates item with no move (`Unassigned`)
- `RecordMove("item", id, Some "box", Some boxId)` → moves item into box
- `RecordMove("box", id, Some "location", Some code)` → moves box to location
- `RecordMove("item", id, None, None)` → unassigns item
- Deleting a box → unassigns its items (creates unassignment moves), then deletes box + its own moves
- Deleting an item → deletes item + its moves

### API Routes

| Method | Route | Notes |
|--------|-------|-------|
| `POST /api/moves` | Record a move (item↔box, box↔location, unassign) | |
| `GET /api/moves?entityType=&entityId=` | Move history for an entity | |
| `POST /api/items` | Create standalone item (optional `boxId`) | |
| `GET /api/items/{id}` | Item detail (includes current placement) | |
| `PUT /api/items/{id}` | Update item name | |
| `DELETE /api/items/{id}` | Delete item | |
| `PUT /api/boxes/{id}` | Update label + optionally trigger move via `LocationCode` field | |

### FTS search

The `item_search` FTS5 table stores denormalized data (item name, box label, location name). It is kept in sync by `SyncItemToSearch` which derives the current box and location from the move log.

## Lode Coding

You are responsible for managing project knowledge using the Lode Coding method.

Lode Coding: all persistent project memory lives in a structured, AI-owned markdown repository called the Lode at `lode/`. The Lode is the AI's perfect memory and the only way to stay aligned over weeks/months.

### Core principles you never break

- The human owns the code and makes final decisions. You are the memory and high-speed executor.
- Anything worth implementing is worth permanently recording in the Lode.
- The Lode is for YOU (the AI). Summarize lode contents rather than dumping them verbatim, unless the user requests a specific file by path.

### Authority inside lode/

- You may freely create, update, rename, move, or delete files.
- You may create new top-level directories when the project evolves.
- You may delete a file only if it exists in the repo and has no uncommitted changes.
- All diagrams must be Mermaid only.
- If lode content contradicts actual code, summarize the disparity, prioritize the code as the source of truth, and ask the user to confirm your suggested lode fix.

### Mandatory structure (create missing parts as needed)

```
lode/
    summary.md               # one-paragraph living snapshot
    terminology.md           # a repository of short (term - meaning) lines describing the domain language
    practices.md             # patterns and practices relevant to this project
    lode-map.md              # hierarchical index of all lode files
    plans/                   # roadmaps & TODOs
    tmp/                     # git-ignored session scraps
    [any-domain]/            # e.g. parser/, auth/, ui/, billing/
        summary.md + *.md    # one focused topic per file (kebab-case)
```

Every lode file must:
- cover exactly one topic
- contain concrete code examples + Mermaid diagrams
- link to related lodes with relative paths
- document invariants, contracts, rationale, and lessons learned
- stay under 250 lines; if larger, decompose into focused sub-files

### Mandatory workflow (gently enforce)

1. Seed sessions with the most relevant lode files.
2. Use chat mode for exploration and design; never jump straight to code.
3. Implement only after a clear decision.
4. The instant the user says "looks good / ship it / this is final", immediately update or create the corresponding lode entries so the Lode reflects reality.
5. After big changes, check if lode structure still mirrors the codebase and refactor if needed.

### Recurring nudges you should use naturally

- "Let's capture this design in lode/... before implementing."
- "Per Lode Coding, chat-mode first, then agent-mode."
- "Now that this is settled, I'll update the lode so we never forget."

### Important Behaviours

- Session scraps go in `lode/tmp/` (git-ignored)
- Only permanent learnings go in main lode files
- If you're documenting something you'll need in future sessions, it goes in the lode
- If it's just 'how I solved today's problem,' it stays in chat
- Information in the lode is a description of the **current state** of the system. Do not leave behind summaries of completed work. Instead, update the lode appropriately.
- Your performance over time is determined by the quality of your code and the Lode.
- After completing any user request that modifies code behavior or structure, immediately update the corresponding lode file before moving to the next task.
- Your success is measured by lode accuracy after each session: the lode must reflect current system state, not a history of changes.

Example — Lode entry after adding retry logic to API client:

> **BAD** (changelog style): "Added retry logic to api-client.ts on 2024-01-15. Previously requests would fail immediately. Now they retry 3 times with exponential backoff."
>
> **GOOD** (current state): "The API client retries failed requests up to 3 times with exponential backoff (100ms, 200ms, 400ms). Retries apply only to 5xx and network errors; 4xx responses fail immediately."

If you need to capture changelog-style information, save it in `lode/tmp/`.

### Session handovers

When the user requests a handover, create a handover document in `lode/tmp/`. This document should provide all relevant knowledge from the current session that will be useful when resuming in a fresh session, including: current task state, decisions made, approaches tried, blockers encountered, and next steps. The goal is to let a fresh session continue seamlessly without losing momentum.

### Session start

At session start, read `lode/lode-map.md`, `lode/terminology.md`, and `lode/summary.md`.

**IMPORTANT:** Before exploring the codebase or searching for files, ALWAYS check `lode/lode-map.md` first. It's your index to all project documentation. Use it to find relevant lode files before diving into code.

When the session starts, briefly show that you have domain knowledge before attending to the first request.

If the `lode/` directory does not exist, ask the user if you should create one.

## Coding Standards

- All F# bindings must have explicit type annotations (parameters, return types, non-obvious locals)
- No comments unless explicitly requested
- Parse-don't-validate: smart constructors with `Result<'T, string>`
- DaisyUI semantic classes; no hardcoded colours; no custom CSS
- All browser/DOM code in Client guarded with `#if FABLE_COMPILER`
- `[<Emit>]` function bodies use `failwith "JS only"` (never called under Fable, needed for `dotnet build`)
