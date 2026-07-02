# Persistence

SQLite database at `data/boxtracker.db` (configurable via `BOXTRACKER_DATA` env var). Access via `Storage` class in `src/Server/Storage.fs`.

## Connection model

`Storage` is registered **per-request (`AddScoped`)** in `Program.fs`; each request gets its own instance holding its own SQLite connection (drawn from Microsoft.Data.Sqlite's default connection pool, returned when the scoped instance is disposed at end of request). Concurrent requests therefore use separate connections instead of serializing on one shared connection.

- `Storage.InitializeSchema(connStr)` runs **once at startup**: enables `journal_mode=WAL` (a persistent DB property) and creates tables + runs idempotent `ALTER TABLE` migrations.
- Per-instance `Connect()` only opens the connection and sets `busy_timeout=5000` (a per-connection setting). It does *not* re-run the schema.
- WAL lets request connections and the background photo worker (`PhotoJobStore`, separate connection) read/write the DB concurrently; `busy_timeout` makes a writer wait briefly for the single write lock instead of failing.

## Write-path transactions

All commands in instance members are created via `this.CreateCommand()`, which attaches the ambient `SqliteTransaction` when one is active (Microsoft.Data.Sqlite requires commands to carry the connection's active transaction). `this.InTransaction(work)` opens a transaction, runs `work`, and commits once; nested calls join the ambient transaction (e.g. `DeleteBox` → `RecordMove`), and an exception rolls back via disposal.

Multi-statement write paths wrapped in `InTransaction` so each commits (and fsyncs) once instead of per statement: `RecordMove`, `AddItem`, `UpdateItemName`, `DeleteItem`, `UpdateBox`, `DeleteBox`, `UpdateLocationName`, `UpdateLocationCode`.

## Tables

| Table | Columns |
|-------|---------|
| `location` | `code` (PK, TEXT), `name`, `is_archived`, `created_at` |
| `box` | `id` (PK, TEXT e.g. "BOX-001"), `label`, `created_at` |
| `item` | `id` (PK, UUID TEXT), `name`, `photo_path`, `added_at` |
| `move` | `id` (PK, UUID), `entity_type` ("item"/"box"), `entity_id`, `to_type` ("box"/"location"/NULL), `to_id` (nullable), `moved_at` |

No FK columns for placement — `item` has no `box_id` column, `box` has no `location_code` column. Placement is derived from the move log.

## How placement is derived

The current placement for any entity is its most recent move (`moved_at DESC`). Storage queries use:
```sql
ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
-- then filter WHERE rn = 1
```
This window function is used in `GetBox`, `GetItem`, `GetItemsForBox`, `ListBoxes`, `GetAssignedBoxCount`, `SearchItems`, `ReindexBoxItems`, `ReindexLocationItems`, and `DeleteBox`.

## FTS5 Search

Regular FTS5 virtual table `item_search` with columns:
- `item_id`, `item_name`, `box_label`, `location_name`
- Tokenizer: `porter unicode61`
- Maintained by application layer (INSERT/DELETE, no UPDATE)

`SyncItemToSearch` derives the current box and location from the move log (calls `GetItemPlacement` → potentially `GetBoxPlacement`) to denormalize into the FTS row.

## FTS5 Sync Points

1. **AddItem** → `RecordMove` (item→box) → `SyncItemToSearch` (via RecordMove)
2. **RecordMove** (item) → `RemoveFromSearch` + `SyncItemToSearch`
3. **RecordMove** (box) → `ReindexBoxItems` (all items in that box get re-synced)
4. **UpdateItemName** → `RemoveFromSearch` + `SyncItemToSearch`
5. **DeleteItem** → `RemoveFromSearch`
6. **DeleteBox** → unassigns items via `RecordMove("item", ..., None, None)` → each triggers `RemoveFromSearch` + `SyncItemToSearch` → then deletes orphaned FTS rows for items that were in the box
7. **UpdateLocationName** → `ReindexLocationItems` (all items in boxes at that location)
8. **UpdateBox** (label) → `ReindexBoxItems`

## Storage API

`SearchResult` record: `{ ItemId, ItemName, PhotoPath, BoxId, BoxLabel, LocationCode, LocationName }`

### Location methods
- `ListLocations(includeArchived: bool)` → `Location list`
- `GetLocation(code: string)` → `Location option`
- `CreateLocation(code: LocationCode, name: LocationName)` → `Location`
- `UpdateLocationName(code: string, name: LocationName)` → `Location option` (also reindexes FTS5)
- `GetAssignedBoxCount(code: string)` → `int` (derived from move log)
- `SetLocationArchived(code: string)` → `unit`

### Box methods
- `ListBoxes(locationCode: string option, unassigned: bool)` → `Box list` (placement derived from move log)
- `GetBox(id: string)` → `Box option` (LEFT JOIN with latest move)
- `CreateBox(label: BoxLabel option)` → `Box` (sequential ID, starts `Unassigned`, no move created)
- `UpdateBox(id: string, label: BoxLabel option)` → `Box option` (label only, reindexes FTS5)
- `DeleteBox(id: string)` → `string list` (unassigns items via moves, deletes box moves, returns photo paths)
- `GetItemsForBox(boxId: string)` → `Item list` (INNER JOIN items whose latest move is that box)

### Item methods
- `CreateItem(name: ItemName, photoPath: PhotoPath option)` → `Item` (standalone, `Unassigned`, no move)
- `AddItem(boxId: string, name: ItemName, photoPath: PhotoPath option)` → `Item` (creates item + move `InBox boxId`)
- `GetItem(id: string)` → `Item option` (LEFT JOIN with latest move)
- `UpdateItemName(id: string, name: ItemName)` → `Item option` (re-syncs FTS5)
- `UpdateItemPhoto(id: string, photoPath: PhotoPath option)` → `Item option` (updates photo_path column only)
- `DeleteItem(id: string)` → `string option` (deletes moves + item, returns photo path)

### Move methods
- `RecordMove(entityType: string, entityId: string, toType: string option, toId: string option)` → `Move` (append-only, syncs FTS5)
- `GetMoveHistory(entityType: string, entityId: string)` → `Move list` (ordered by `moved_at DESC`)

### Search
- `SearchItems(query: string option)` → `SearchResult list` (FTS5 MATCH if query present; all items if not)

## Photos

Flat files at `data/photos/{boxId}/{guid}.{ext}`. DB stores relative path only. Handlers responsible for file I/O using paths returned by `DeleteBox`/`DeleteItem`.

See also: [types.md](types.md) for domain types, [api.md](api.md) for API routes.
