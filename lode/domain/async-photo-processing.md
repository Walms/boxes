# Async Photo Processing

Photo uploads are split into two phases — a fast **upload** and a slower
server-side **processing** step — so the request returns quickly and resizing
continues even if the client disconnects or the server restarts.

Related: [image-optimization.md](image-optimization.md) (the actual resize/WebP
encoding), [persistence.md](persistence.md), [api.md](api.md).

## Why

`ImageProcessing.processUploadedImage` (decode + resize to 3500×3500 and
250×250 + WebP encode, twice) can take several seconds for a phone photo.
Previously this ran inline in the HTTP request, so the upload felt slow and a
dropped connection lost the work. Now the raw bytes are persisted and a durable
job drives processing on a background worker.

## Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant H as Upload handler
    participant DB as photo_job (SQLite)
    participant W as PhotoProcessingService
    C->>H: POST /api/boxes/{id}/photo (multipart)
    H->>DB: save raw to data/photo-jobs/{guid}, INSERT job (pending)
    H-->>C: 202 { id, status: "pending" }
    H->>W: PhotoJobSignal.Notify()
    loop until completed/failed
        C->>H: GET /api/photo-jobs/{jobId}
        H-->>C: { status }
    end
    W->>DB: ClaimNext() -> processing
    W->>W: processUploadedImage(raw -> full/thumb webp)
    W->>DB: SetEntityPhoto(); delete old variants; MarkCompleted
    C->>H: GET /api/photo-jobs/{jobId} -> completed
    C->>C: reload page, new photo shows
```

## Server pieces

- `src/Server/Schema.fs` — `photo_job` table (`id, entity_type, entity_id,
  status, error, source_path, photo_path, old_photo_path, created_at,
  updated_at`) + `idx_photo_job_status`.
- `src/Server/PhotoJobStore.fs` — owns its **own** SQLite connection (separate
  from request-path `Storage`); gated by an internal `lock`. Key members:
  `CreateJob`, `GetJob`, `ClaimNext` (oldest pending → processing, atomic),
  `MarkCompleted`, `MarkFailed`, `SetEntityPhoto` (raw `UPDATE` of
  box/item/location `photo_path`), `ResetInterrupted` (processing → pending on
  startup).
- `src/Server/PhotoProcessing.fs` — `PhotoJobSignal` (a `SemaphoreSlim(0,1)`
  wake-up) and `PhotoProcessingService : BackgroundService`. On start it calls
  `ResetInterrupted`, then loops: drain all pending jobs, else wait on the
  signal (2s timeout fallback). One worker → sequential processing.
- `src/Server/Handlers/PhotoJobHandlers.fs` — `enqueuePhotoJob` (writes raw to
  `data/photo-jobs/{guid}` **outside** the web-served `photos/` dir, inserts the
  job, signals the worker) and `getPhotoJob` (status endpoint).
- Upload handlers (`BoxHandlers.uploadBoxPhoto`/`addItem`,
  `ItemHandlers.updateItemPhoto`, `LocationHandlers.uploadLocationPhoto`) now
  return `202` with a `PhotoJobResponse` instead of processing inline.
  `addItem` returns `AddItemResponse { Item; PhotoJobId option }`.

## Concurrency / durability invariants

- **Two connections, WAL.** `Storage` and `PhotoJobStore` each open their own
  connection; both run `PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;` so
  the worker and request handlers can read/write concurrently without "database
  is locked".
- **Old photo kept until success.** `old_photo_path` is deleted only after the
  new variants are written and `photo_path` is updated, so the entity keeps
  showing its previous photo while processing.
- **Resume after restart.** Raw upload lives on disk and the job row survives;
  `ResetInterrupted` re-queues anything stuck in `processing`.
- **Statuses:** `pending` → `processing` → `completed` | `failed`.

## Client pieces

- `src/Client/Api.fs` — `PhotoJobDto`, `AddItemResultDto`, `getPhotoJob`;
  upload functions now return `PhotoJobDto`.
- `src/Client/State.fs` — fields `PhotoProcessing: bool`, `PhotoJobId: string
  option`. Messages `PhotoUploadStarted`, `PollPhotoJob`, `PhotoJobPolled`.
  Upload → `UploadingPhoto`; after `202` → `PhotoProcessing` + poll every 800ms
  (`schedulePollCmd`); on `completed` reload the current page; on `failed` show
  the error. Navigating away clears tracking (`resetPageState`) but the server
  keeps processing.
- `src/Client/Pages/Common.fs` (`photoStatusBanner`) — the spinner shows "Uploading…" then "Processing…".

## Lessons / gotchas

- `BackgroundService.ExecuteAsync` must return `Task`; the F# `task { }` builder
  yields `Task<unit>`, so the body needs `:> Task`.
- Raw uploads must stay outside `data/photos/` or they'd be downloadable via the
  `/api/photos` static middleware.
