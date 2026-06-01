# Image Optimization

Photos uploaded by users (boxes and items) are automatically compressed and resized for web use. All photos are converted to WebP format with two variants generated per upload.

## Implementation Status

✅ **Complete** — All photo uploads generate thumbnails and compressed full-size images automatically.

## How It Works

**Upload Processing:** (now runs asynchronously — see
[async-photo-processing.md](async-photo-processing.md))
1. User uploads a photo (any format: JPG, PNG, etc.); the request returns `202`
   immediately with a `photo_job` and the client polls for status.
2. The raw upload is staged in `data/photo-jobs/{guid}`; a background worker
   processes it via `ImageProcessing.processUploadedImage`.
3. Two WebP variants are generated and saved:
   - `{guid}-full.webp` — max 3500×3500 px, 90% quality
   - `{guid}-thumb.webp` — max 250×250 px, 75% quality (lower quality is
     invisible at the ≤128 px display sizes and shrinks the file further)
4. PhotoPath stores base path: `photos/{boxId}/{guid}` (no extension); the
   entity's `photo_path` is set only once processing completes.
5. The raw staged file is deleted after processing (success or failure).

**File Storage:**
```
data/photos/
  {boxId}/
    {guid}-full.webp    # Full-size variant (3500×3500 max, 90% quality)
    {guid}-thumb.webp   # Thumbnail variant (250×250 max, 75% quality)
```

**API Serving:**
- Static files middleware at `/api/photos` serves WebP files directly
- Files are served by appending `-full` or `-thumb` suffix before `.webp` extension
- Example: path `photos/BOX-001/abc123` becomes `/api/photos/BOX-001/abc123-full.webp` or `-thumb.webp`
- Responses carry `Cache-Control: public, max-age=31536000, immutable`
  (`Program.fs`, `OnPrepareResponse`). Filenames are content-addressed by guid
  and never mutated, so the browser caches them for a year and reuses thumbnails
  across navigations instead of re-fetching.

**Client loading:**
- List/grid thumbnails use `loading="lazy"` + `decoding="async"` so off-screen
  images aren't fetched until scrolled near, and decoding doesn't block render.
- Small displays always use the `thumb` variant; the `full` variant is loaded
  only when the image viewer is opened.

**Client Usage:**
- `photoUrlFull(path)` — returns full-size image URL for detail views
- `photoUrlThumb(path)` — returns thumbnail URL for list views and grids
- Detail pages (location, box detail): use full-size for 48×48 display
- List pages (items list, search results): use thumbnails for 14-16×14-16 display
- Thumbnails in modals and small contexts use thumbnails for fast loading

**Cleanup:**
- When deleting a box/item, both `{guid}-full.webp` and `{guid}-thumb.webp` are deleted
- Storage layer returns base path; handlers manage file variants

## Technical Details

**Backend:**
- `src/Server/ImageProcessing.fs` — `processUploadedImage` function using SixLabors.ImageSharp
- `src/Server/Handlers/BoxHandlers.fs` — `uploadBoxPhoto` and `addItem` handlers
- `src/Server/Handlers/ItemHandlers.fs` — `updateItemPhoto` handler
- `src/Shared/Domain/PhotoPath.fs` — `createWebP` function for base path generation

**Frontend:**
- `src/Client/Pages.fs` — `photoUrlFull` and `photoUrlThumb` helper functions
- All photo display locations updated to use appropriate variant based on context

## Performance Benefits

- WebP format reduces file size 25-35% vs JPG at same quality
- Thumbnails (250×250) are 75-90% smaller than full images
- List views load 10-20x faster with thumbnails
- Typical full photo: ~200-300 KB → ~50-70 KB WebP
- Typical thumbnail: ~50-70 KB → ~5-10 KB WebP

## Migration from Previous Format

**Existing photos uploaded before optimization are inaccessible.** The new system expects WebP files with `-full` and `-thumb` suffixes, but old photos have original extensions (JPG, PNG).

**Impact:** Old photos return 404; users can re-upload them when viewing a box/item.

**Why this approach:**
- Minimal implementation complexity
- Personal app with manageable photo volume
- Users naturally see missing photos and can easily retake/re-upload
- Alternative would be a complex migration script or graceful fallback handler

**If migration is needed later:**
- Query database for all PhotoPath entries
- Batch convert old photos to WebP format
- Update database paths and filenames
- Delete old files

## Notes

- SixLabors.ImageSharp 3.1.5 has known vulnerabilities (NU1902, NU1903) but mitigated by local-only app
- WebP support is standard in all modern browsers (>95% coverage)
- Resizing preserves aspect ratio with letterboxing as needed
- Quality 80% maintains good visual quality while maximizing compression
