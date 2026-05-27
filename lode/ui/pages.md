# Client Pages

Hash-based SPA routing (`#/path`). Pages defined in `State.fs` as `Page` DU, views in `Pages.fs`.

## Navigation

Navbar links: **Locations** | **Boxes** | **Items** | **Search**

## Pages

| Page | Hash | Description |
|------|------|-------------|
| `LocationsList` | `#/locations` | Grid of location cards with create form. Default landing page. |
| `LocationDetail` | `#/locations/{code}` | Location info, edit name, archive, assigned boxes grid. |
| `BoxesList` | `#/boxes` | Grid of box cards with create form. Filter by location dropdown. |
| `BoxDetail` | `#/boxes/{id}` | Box info, edit label, assign to location, item list with add/move/delete. Photo upload for items. |
| `ItemsList` | `#/items` | All items with create standalone form (name + optional box). Inline edit name, move to box, delete. |
| `ItemsSearch` | `#/items/search` | Debounced FTS5 search with photo thumbnails and location info. Read-only. |

## Items Page

The Items page (`#/items`) provides full item management outside the box context:

- **List**: Shows all items (up to 100) via `GET /api/items` (no `q` param). Each card shows photo, name, box/location, and action buttons.
- **Create**: Standalone item creation with optional box assignment via `POST /api/items`. Uses `Boxes` state field for the dropdown.
- **Edit**: Inline name editing via `PUT /api/items/{id}`. Replaces name text with input + save/cancel. Also shows a file input for photo upload — selecting a photo immediately uploads via `PUT /api/items/{id}/photo` (separate from the name save).
- **Move**: Modal dialog to move item to any box via `POST /api/moves`. Loads all boxes into `BoxesForItemMove`.
- **Delete**: Immediate delete via `DELETE /api/items/{id}`.

## Key Patterns

- Each page resets its state on navigation via `resetPageState`
- Forms are inline collapsible sections (not separate pages)
- Move dialogs are modal overlays using DaisyUI `modal-open`
- `Cmd.OfAsync.either` for all API calls with `ErrorOccurred` fallback
- Edit flows: `StartEdit*` stores current value → `Edit*Changed` updates → `SubmitEdit*` sends API → `*Updated` navigates to refresh
