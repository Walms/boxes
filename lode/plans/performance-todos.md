# Performance TODOs

Remaining items from the June 2026 performance review. Already done: Caddy
bcrypt cost lowered 14 → 10 (Caddy 2.11.3 also caches verifications), the
client no longer refetches whole pages after mutations (in-place state updates
in `src/Client/State.fs`), and the item detail page now fetches a single item
via `GET /api/items/{id}` (which returns the search-result shape with
box/location names) instead of listing all items.

## TODO 2 — Multi-statement write paths run without a transaction

In `src/Server/Storage.fs`:

- `RecordMove` on a box calls `ReindexBoxItems`, which loops over every item
  in the box doing ~4 separate statements each (`RemoveFromSearch`, name
  lookup, placement lookups, FTS insert).
- `DeleteBox` records one unassignment move per contained item the same way,
  plus per-item photo-path lookups.
- `UpdateLocationName` / `UpdateLocationCode` call `ReindexLocationItems`
  with the same per-item loop.

Each statement outside a transaction commits (and fsyncs) on its own, which
is slow on cloud-VPS disks. Fix: wrap each of these operations in a single
`SqliteTransaction` so a box move/delete is one commit. Watch out:
`UpdateLocationCode` already opens its own transaction — the reindex happens
after commit and would need to join or follow it.

## TODO 3 — Image pipeline does redundant work per upload

In `src/Server/ImageProcessing.fs` (`processUploadedImage`):

- The decoded original is cloned twice (once for the full variant, once for
  the thumb). Since the client caps uploads at 1920px, resize once for the
  full variant and derive the 250px thumbnail from that resized image —
  halves decode/clone memory and CPU.
- `makeProgressive` (jpegtran process spawn) runs on the thumbnail too.
  Progressive encoding is pointless at 250px — skip it for thumbs and save a
  process spawn per upload.
- Note: the 3500px full-size cap is effectively dead code because the client
  compresses to ≤1920px before upload (`compressImageJs` in
  `src/Client/Api.fs`).
