# API Handlers

All API handlers are in `src/Server/Handlers/`. DTO types and mapping functions are in `src/Server/Dto.fs`. Handlers access Storage via `ctx.GetService<Storage>()` and config via `ctx.GetService<BoxTrackerConfig>()`.

## DI Registration

`BoxTrackerConfig` registered as singleton in `Program.fs` with `{ DataDir = dataDir }`. `Storage` also registered as singleton.

## JSON Serialization

`System.Text.Json` with `PropertyNameCaseInsensitive = true` (accepts any casing on input) but no `CamelCase` naming policy. Response properties are PascalCase to match Fable record field names. Client DTOs use `array` for collection fields.

## DTO Pattern

Domain types use private DU constructors (not JSON-serializable). Handlers map domain types to plain record DTOs via functions in `Dto.fs`:
- `locationToDto`, `boxToDto`, `itemToDto`, `searchResultToDto`, `moveToDto`
- `boxToDto` extracts `LocationCode` from `box.Placement` (Container DU)
- `itemToDto` extracts `BoxId` from `item.Placement` (Container DU)
- `moveToDto` converts `Container` back to `ToType`/`ToId` strings

Request DTOs: `CreateLocationRequest`, `UpdateLocationRequest`, `CreateBoxRequest`, `UpdateBoxRequest`, `CreateItemRequest`, `UpdateItemRequest`, `MoveRequest`

Composite responses: `LocationDetailResponse` (location + boxes), `BoxDetailResponse` (box + items)

## Validation

Smart constructors validate all input at the API boundary. Use fully qualified paths (`BoxTracker.LocationCode.create`) to avoid module/type name ambiguity.

## Error Responses

- 400: `{ "error": "validation message" }`
- 404: `{ "error": "Resource 'X' not found" }`
- 201: returned for POST creates

## Endpoint Summary

| Method | Route | Handler | Notes |
|--------|-------|---------|-------|
| GET | `/api/locations` | `listLocations` | `?includeArchived=true` |
| POST | `/api/locations` | `createLocation` | validates code + name |
| GET | `/api/locations/{code}` | `getLocation` | returns location + assigned boxes |
| PUT | `/api/locations/{code}` | `updateLocation` | validates name |
| DELETE | `/api/locations/{code}` | `archiveLocation` | uses `Location.tryMakeEmpty` guard |
| GET | `/api/boxes` | `listBoxes` | `?location=`, `?unassigned=true` |
| POST | `/api/boxes` | `createBox` | validates optional label |
| GET | `/api/boxes/{id}` | `getBox` | returns box + items |
| PUT | `/api/boxes/{id}` | `updateBox` | validates label; if `LocationCode` changes, creates a move record |
| DELETE | `/api/boxes/{id}` | `deleteBox` | unassigns items (creates moves), deletes box + its moves, removes photo files |
| POST | `/api/boxes/{id}/items` | `addItem` | multipart form: `name` + optional `photo`; creates item + move |
| PUT | `/api/boxes/{id}/items/{itemId}` | `updateItem` | validates name |
| DELETE | `/api/boxes/{id}/items/{itemId}` | `deleteItem` | deletes item + its moves, removes photo file |
| POST | `/api/items` | `createItem` | standalone item (optional `boxId` in JSON body) |
| GET | `/api/items/{id}` | `getItem` | returns `SearchResultResponse` (item + box label + location code/name) |
| PUT | `/api/items/{id}` | `updateItemStandalone` | validates name |
| POST | `/api/items/{id}/photo` | `updateItemPhoto` | multipart: file `photo`; replaces existing photo |
| DELETE | `/api/items/{id}` | `deleteItemStandalone` | deletes item + its moves |
| GET | `/api/items` | `searchItems` | `?q=` for FTS5 search; no `q` returns all items (limit 100) |
| POST | `/api/moves` | `recordMove` | `MoveRequest { EntityType, EntityId, ToType, ToId }` |
| GET | `/api/moves` | `getMoveHistory` | `?entityType=&entityId=` |

## Photo Upload

`addItem` expects `multipart/form-data` with field `name` (required) and file `photo` (optional). Photos saved to `{dataDir}/photos/{boxId}/{guid}.{ext}`.

`updateItemPhoto` expects `multipart/form-data` with file `photo`. Deletes old photo file if exists, saves new one. Photo folder uses the item's current box ID if assigned, otherwise the item's own GUID.

## Photo Cleanup

`deleteBox` and `deleteItem`/`deleteItemStandalone` handlers receive relative photo paths from Storage and delete files from `{dataDir}`.

## Labels

Label rendering in `src/Server/Labels.fs`. QRCoder generates inline SVG QR codes. Labels are self-contained HTML pages with `@media print` CSS for 100mm × 60mm `@page` dimensions. Box labels show current location (code + name) when assigned.

| Method | Route | Handler | Notes |
|--------|-------|---------|-------|
| GET | `/api/boxes/{id}/label` | `boxLabel` | single box label, includes location if assigned |
| GET | `/api/locations/{code}/label` | `locationLabel` | single location label |
| GET | `/api/boxes/labels` | `batchBoxLabels` | `?ids=BOX-001,BOX-002` batch print |

Each label page includes a "Print" button (hidden via `@media print`). Batch labels use `page-break-after: always`.

See also: [persistence.md](persistence.md) for Storage API, [types.md](types.md) for domain types.
