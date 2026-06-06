# Client Pages

Hash-based SPA routing (`#/path`). Pages defined in `State.fs` as `Page` DU. Views live under `src/Client/Pages/`, split by area: `Common.fs` (shared helpers — `navbar`, `breadcrumb`, `photoUrl*`, `imageViewer`, `loadingSpinner`, `photoStatusBanner`, `historyModal`, the QR `scannerModal`/`QrScannerComponent`), `Locations.fs`, `Boxes.fs`, `Items.fs` (each holds that area's pages plus its own modal dialogs), and `Router.fs` (`renderPage` dispatch on the `Page` DU). All five are nested modules under the `BoxTracker.Client.Pages` namespace; compile order is Common → Locations → Boxes → Items → Router.

## Navigation

Navbar links: **Locations** | **Boxes** | **Items** | **Search**

- Desktop (`md+`): horizontal menu in navbar
- Mobile (`< md`): hamburger dropdown (`☰`) using DaisyUI `dropdown` component

## Pages

| Page | Hash | Description |
|------|------|-------------|
| `LocationsList` | `#/locations` | Grid of location cards with create form. Default landing page. |
| `LocationDetail` | `#/locations/{code}` | Location info, edit name, archive, boxes grid with add/remove box. |
| `BoxesList` | `#/boxes` | Grid of box cards with create form. Filter by location dropdown. |
| `BoxDetail` | `#/boxes/{id}` | Box info, edit label, assign to location, item list with add/move/delete, add existing item. Photo upload for items. |
| `ItemsList` | `#/items` | All items with create standalone form (name + optional box). Inline edit name, move to box, delete. |
| `ItemsSearch` | `#/items/search` | Debounced FTS5 search with photo thumbnails and location info. Read-only. |

## Items Page

The Items page (`#/items`) provides full item management outside the box context:

- **List**: Shows all items (up to 100) via `GET /api/items` (no `q` param). Each card shows photo, name, box/location, and action buttons.
- **Create**: Standalone item creation with optional box assignment via `POST /api/items`. Uses `Boxes` state field for the dropdown.
- **Edit**: Inline name editing via `PUT /api/items/{id}`. Replaces name text with input + save/cancel. Also shows a file input for photo upload — selecting a photo immediately uploads via `PUT /api/items/{id}/photo` (separate from the name save).
- **Move**: Modal dialog to move item to any box via `POST /api/moves`. Loads all boxes into `BoxesForItemMove`.
- **Unassign**: Button (shown only for assigned items) to remove item from its box via `POST /api/moves` with empty `ToType`/`ToId`.
- **Delete**: Immediate delete via `DELETE /api/items/{id}`.

## Box Detail Page

The Box Detail page (`#/boxes/{id}`) manages a single box and its items:

- **Header**: Box label (editable), delete box button.
- **Location assignment**: Dropdown to assign/unassign box to a location.
- **Add new item**: Inline form with name + optional photo, creates item directly in box via `POST /api/boxes/{id}/items`.
- **Add existing item**: Modal listing all unassigned items (from `GET /api/items`, filtered to `BoxId` empty). User selects an item, confirms to move it into the box via `POST /api/moves`. Uses `AddingExistingItem`, `UnassignedItems`, `SelectedExistingItemId` state fields.
- **Item list**: Shows all items in the box with Move, Unassign (remove from box), and Delete actions.

## Location Detail Page

The Location Detail page (`#/locations/{code}`) manages a single location and its boxes:

- **Header**: Location name (editable) with code badge, edit/archive buttons.
- **Add box**: Modal listing all boxes not already at this location (fetched via `GET /api/boxes`, filtered to exclude boxes with `LocationCode = currentCode`). User selects a box, confirms to move it via `POST /api/moves` (`moveEntity "box" boxId "location" code`). Uses `AddingBoxToLocation`, `BoxesForLocationMove`, `SelectedBoxForLocationMove` state fields.
- **Remove box**: Each box card has a Remove button that unassigns it from the location via `unassignEntity "box" boxId` (`POST /api/moves` with empty `ToType`/`ToId`).
- **Box cards**: Clickable box name navigates to `BoxDetail`.

## History Modal

All boxes and items have a history timeline accessible via "View History" in their action menus (Actions ▾ or ⋮):

- **Trigger**: "View History" in box Actions ▾ dropdown (box detail page); "View History" in item ⋮ dropdown (box detail and items pages).
- **Data source**: `GET /api/moves?entityType=&entityId=` returns `MoveDto array` ordered newest-first; the modal reverses to show oldest-first chronology.
- **Timeline events**: Creation timestamp (`CreatedAt`/`AddedAt`, if available) as first event, then each move. Move descriptions: "Moved to {location name}" for boxes (looks up name from `AvailableLocations`), "Moved to {box id}" for items, "Unassigned" for removals.
- **State fields**: `ShowHistoryModal`, `HistoryTitle`, `HistoryEntityType`, `HistoryEntityId`, `HistoryCreatedAt: string option`, `HistoryMoves`, `HistoryLoading`.
- **Messages**: `ShowHistory (entityType, entityId, title, createdAt option)` → triggers API fetch; `HistoryLoaded`, `CloseHistory`.
- **Rendered**: `historyModal` component above all pages in `renderPage` (next to `imageViewer`).
- Reset on navigation via `resetPageState`.

## Full-Screen Image Viewer

All images in the app (box photos, item photos) are clickable and open a full-screen viewer. The viewer:

- Displays images centered on a dark background
- Allows native browser pinch-zoom on mobile (no custom zoom logic needed)
- Closes via close button (✕) in top-right, click outside the image, or device back button
- Uses state field `ViewingImageUrl: string option` to track the current image
- Dispatches `ShowImageViewer url` on image click, `CloseImageViewer` to dismiss
- Rendered via `imageViewer` component above the main content (always available)

Images with hover effect get `cursor-pointer` and `opacity-80 transition-opacity` to indicate clickability.

## Key Patterns

- Each page resets its state on navigation via `resetPageState`
- Forms are inline collapsible sections (not separate pages)
- Move dialogs are modal overlays using DaisyUI `modal-open`
- `Cmd.OfAsync.either` for all API calls with `ErrorOccurred` fallback
- Edit flows: `StartEdit*` stores current value → `Edit*Changed` updates → `SubmitEdit*` sends API → `*Updated` navigates to refresh
