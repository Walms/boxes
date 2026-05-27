# BoxTracker — Requirements Document

**Version:** 0.3  
**Status:** Iterating

---

## Revision History

| Version | Changes |
|---------|---------|
| 0.1 | Initial draft |
| 0.2 | Removed box lifecycle states (Open/Sealed/Delivered). Removed event sourcing. Renamed ContentEntry → Item. Added Items screen (list + search). QR code encodes Box ID only (not a URL). Browser-side photo scaling. Upgraded search to SQLite FTS5. Resolved open questions 1, 3, 4, 5, 6. |
| 0.3 | Added coding rule: explicit type annotations on all F# bindings. Added DaisyUI to frontend stack. Set label dimensions to 100 mm × 60 mm. |

---

## 1. Purpose

BoxTracker is a lightweight home moving assistant for tracking packed boxes and their
garage locations. It produces printable QR labels for boxes and locations, records items
per box with optional photos, and provides fast search across all items.

This document is a living specification. Sections marked **[TBD]** are placeholders for
decisions not yet made.

---

## 2. Scope

The system is deliberately narrow:

- A **Location** is a named zone in the garage (e.g. "Back Wall Left", "Centre Stack A").
- A **Box** is a physical container, assigned to at most one location at a time.
- An **Item** is a named thing inside a box, with an optional photo.
- QR labels for boxes and locations are printable from the browser.

Out of scope: multi-user access, authentication, reminders, move scheduling.

---

## 3. Functional Requirements

### 3.1 Location Management

| ID   | Requirement |
|------|-------------|
| L-01 | The user can create a Location with a short code and a human-readable name. |
| L-02 | The short code must be unique, non-empty, and contain only letters, digits, and hyphens. It is stored and displayed in uppercase. |
| L-03 | The user can view a list of all locations. |
| L-04 | The user can view a single location, including all boxes currently assigned to it. |
| L-05 | The user can print a label for a location. The label displays the location name, short code, and a QR code encoding the location code as plain text. |
| L-06 | The user can archive a location. Archived locations are hidden by default but remain queryable. A location with currently assigned boxes cannot be archived. |
| L-07 | The user can edit a location's name. The short code is immutable after creation. |

### 3.2 Box Management

| ID   | Requirement |
|------|-------------|
| B-01 | The user can create a Box. The system assigns a unique, sequential Box ID (e.g. `BOX-001`). IDs are never reused. |
| B-02 | The user can optionally add a short descriptive label to a box at creation or edit time (e.g. "Kitchen utensils"). |
| B-03 | The user can assign a Box to a Location. |
| B-04 | The user can unassign a Box from its current Location. |
| B-05 | The user can view a list of all boxes, filterable by location and by assignment status (assigned / unassigned). |
| B-06 | The user can view a single box, showing its location (if any), all items, and photo thumbnails. |
| B-07 | The user can print a label for a box. The label displays the Box ID, the optional descriptive label, and a QR code encoding the Box ID as plain text (e.g. `BOX-042`). |
| B-08 | The user can delete a Box. All associated items and their photos are also deleted. |

### 3.3 Item Management

| ID   | Requirement |
|------|-------------|
| I-01 | The user can add an Item to a Box. Each item has a name (required) and an optional photo. |
| I-02 | Photos are uploaded from the device (camera or file picker). |
| I-03 | Photos are stored as flat files on the server under the path `data/photos/{boxId}/{guid}.{ext}`. |
| I-04 | The photo path stored in the database is relative, never an absolute filesystem path. |
| I-05 | Photos are scaled and displayed by the browser; the server stores the original file as uploaded. |
| I-06 | The user can edit an item's name. |
| I-07 | The user can delete an Item. If the item has an associated photo, the file is also deleted from disk. |
| I-08 | The user can view an Items screen listing all items across all boxes, showing item name, box ID, box label, and location (if assigned). |

### 3.4 Search

| ID   | Requirement |
|------|-------------|
| S-01 | The user can search from the Items screen. The search query matches across item names, box labels, and location names. |
| S-02 | Search is implemented using SQLite FTS5 for ranked full-text matching. |
| S-03 | Search results display item name, the box it belongs to (ID + label), and the box's current location. |
| S-04 | Search results are ranked by relevance (FTS5 `bm25` rank). |
| S-05 | An empty query returns all items, ordered by most recently added. |
| S-06 | Search is triggered on input change with a short debounce (≥ 300 ms); no submit button required. |

### 3.5 Label Printing

| ID   | Requirement |
|------|-------------|
| P-01 | Labels are rendered as a printable browser page (HTML/CSS `@media print`). |
| P-02 | The QR code on a box label encodes the Box ID as plain text (e.g. `BOX-042`). No URL. |
| P-03 | The QR code on a location label encodes the Location Code as plain text (e.g. `BACK-LEFT`). No URL. |
| P-04 | Label dimensions are 100 mm wide × 60 mm tall. Print CSS uses `@page` with explicit dimensions and zero margins. Label content must be fully contained within this area. |
| P-05 | The user can print multiple box labels in a single print job (batch print). |

---

## 4. Domain Model

### 4.1 Core Types

The following F# types encode the domain. Impossible states are unrepresentable: private
constructors ensure all values are valid by construction.

```fsharp
// ── Identifiers ───────────────────────────────────────────────────────────────

/// Validated location short code. Uppercase letters, digits, and hyphens only.
/// Max 20 characters. E.g. "BACK-LEFT".
type LocationCode = private LocationCode of string

/// Sequential, human-readable box identifier. Format: "BOX-NNN" where NNN is
/// zero-padded to at least 3 digits. E.g. "BOX-001", "BOX-042".
type BoxId = private BoxId of string

// ── Value Objects ─────────────────────────────────────────────────────────────

/// Non-empty location display name.
type LocationName = private LocationName of string

/// Optional free-text descriptive label on a box. Non-empty if present.
type BoxLabel = private BoxLabel of string

/// Non-empty item name.
type ItemName = private ItemName of string

/// Relative path to a stored photo file. Never an absolute path.
/// Format: "photos/{boxId}/{guid}.{ext}"
type PhotoPath = private PhotoPath of string

// ── Location Assignment ───────────────────────────────────────────────────────
//
// A box is either unassigned or assigned to exactly one location.

type Assignment =
    | Unassigned
    | AssignedTo of LocationCode

// ── Aggregate Roots ───────────────────────────────────────────────────────────

type Location = {
    Code       : LocationCode
    Name       : LocationName
    IsArchived : bool
    CreatedAt  : System.DateTimeOffset
}

type Item = {
    Id        : System.Guid
    BoxId     : BoxId
    Name      : ItemName
    Photo     : PhotoPath option
    AddedAt   : System.DateTimeOffset
}

type Box = {
    Id         : BoxId
    Label      : BoxLabel option
    Assignment : Assignment
    CreatedAt  : System.DateTimeOffset
}
```

### 4.2 Parse-Don't-Validate

All value-object constructors are private. Smart constructors return `Result<'T, string>`
and are the sole entry point for untrusted input. Nothing inside the domain layer calls
`String.IsNullOrEmpty` or throws exceptions.

```fsharp
module LocationCode =
    let create (raw: string) : Result<LocationCode, string> =
        if System.String.IsNullOrWhiteSpace raw then
            Error "Location code must not be empty"
        elif raw.Length > 20 then
            Error "Location code must be 20 characters or fewer"
        elif raw |> Seq.forall (fun c -> System.Char.IsLetterOrDigit c || c = '-') |> not then
            Error "Location code may only contain letters, digits, and hyphens"
        else
            Ok (LocationCode (raw.ToUpperInvariant()))

    let value (LocationCode s) = s

module ItemName =
    let create (raw: string) : Result<ItemName, string> =
        let trimmed = if raw = null then "" else raw.Trim()
        if trimmed.Length = 0 then Error "Item name must not be empty"
        elif trimmed.Length > 200 then Error "Item name must be 200 characters or fewer"
        else Ok (ItemName trimmed)

    let value (ItemName s) = s
```

Validation errors surface at the API/UI boundary. The domain layer only operates on
already-parsed values.

### 4.3 Business Rules as Types

```fsharp
/// Archiving a location requires proof it has no boxes assigned.
/// The type makes it impossible to call the archive function without
/// first demonstrating the location is empty.
type EmptyLocation = private EmptyLocation of Location

module Location =
    let tryMakeEmpty (location: Location) (assignedBoxCount: int) : Result<EmptyLocation, string> =
        if assignedBoxCount > 0 then
            Error $"Cannot archive '{LocationCode.value location.Code}': {assignedBoxCount} box(es) still assigned"
        else
            Ok (EmptyLocation location)

    let archive (EmptyLocation loc) : Location =
        { loc with IsArchived = true }
```

---

## 5. Technical Architecture

### 5.1 Stack

| Layer        | Technology |
|--------------|------------|
| Backend      | F# / Saturn (Giraffe) |
| Frontend     | F# / Fable + Elmish + Feliz |
| UI components | DaisyUI (component library) on Tailwind CSS |
| Shared types | F# shared project (compiled to both .NET and JS via Fable) |
| Database     | SQLite via Donald or Dapper.FSharp |
| Full-text search | SQLite FTS5 |
| Photos       | Flat files; path `data/photos/{boxId}/{guid}.{ext}` |
| Photo display | Browser-side scaling via CSS (`max-width`, `object-fit`) |
| QR codes     | Client-side: `qrcode` npm package via Fable binding |
| Labels       | Browser print via `@media print` CSS |
| Testing      | xUnit + FsCheck (property tests) |

### 5.2 Project Structure

```
BoxTracker.sln
├── src/
│   ├── Shared/          # Domain types, validation, smart constructors — no I/O
│   ├── Server/          # Saturn API, SQLite persistence, file storage
│   └── Client/          # Fable/Elmish SPA
├── tests/
│   ├── Domain.Tests/    # Unit + property tests against Shared layer
│   └── Api.Tests/       # Integration tests against a temp SQLite file
└── data/
    └── photos/          # Runtime flat file storage (gitignored)
```

### 5.3 Persistence

Simple CRUD tables. No event sourcing.

```sql
CREATE TABLE location (
    code        TEXT PRIMARY KEY,           -- LocationCode; uppercase
    name        TEXT NOT NULL,
    is_archived INTEGER NOT NULL DEFAULT 0,
    created_at  TEXT NOT NULL               -- ISO-8601 with timezone
);

CREATE TABLE box (
    id              TEXT PRIMARY KEY,       -- e.g. "BOX-001"
    label           TEXT,                   -- nullable
    location_code   TEXT REFERENCES location(code),  -- NULL = unassigned
    created_at      TEXT NOT NULL
);

CREATE TABLE item (
    id          TEXT PRIMARY KEY,           -- UUID
    box_id      TEXT NOT NULL REFERENCES box(id) ON DELETE CASCADE,
    name        TEXT NOT NULL,
    photo_path  TEXT,                       -- relative path or NULL
    added_at    TEXT NOT NULL
);

-- FTS5 index for search across item names, box labels, and location names.
-- Kept in sync via application layer (insert/update/delete on item writes).
CREATE VIRTUAL TABLE item_search USING fts5 (
    item_id     UNINDEXED,
    item_name,
    box_label,
    location_name,
    content     = '',          -- contentless; app maintains it
    tokenize    = 'porter unicode61'
);
```

**Search query:**
```sql
SELECT
    i.id, i.name, i.photo_path,
    b.id AS box_id, b.label AS box_label,
    l.code AS location_code, l.name AS location_name
FROM item_search s
JOIN item i ON i.id = s.item_id
JOIN box  b ON b.id = i.box_id
LEFT JOIN location l ON l.code = b.location_code
WHERE item_search MATCH ?
ORDER BY rank
LIMIT 100;
```

### 5.4 API Surface

| Method | Route | Description |
|--------|-------|-------------|
| GET    | `/api/locations` | List locations (query: `?includeArchived=true`) |
| POST   | `/api/locations` | Create location |
| GET    | `/api/locations/{code}` | Location detail + assigned boxes |
| PUT    | `/api/locations/{code}` | Update location name |
| DELETE | `/api/locations/{code}` | Archive location |
| GET    | `/api/boxes` | List boxes (query: `?location=`, `?unassigned=true`) |
| POST   | `/api/boxes` | Create box |
| GET    | `/api/boxes/{id}` | Box detail + items |
| PUT    | `/api/boxes/{id}` | Update box label or assignment |
| DELETE | `/api/boxes/{id}` | Delete box and all items |
| POST   | `/api/boxes/{id}/items` | Add item (multipart: name + optional photo) |
| PUT    | `/api/boxes/{id}/items/{itemId}` | Update item name |
| DELETE | `/api/boxes/{id}/items/{itemId}` | Delete item (and photo file) |
| GET    | `/api/items?q=` | Full-text search across items |
| GET    | `/api/boxes/{id}/label` | Printable label page for a box |
| GET    | `/api/locations/{code}/label` | Printable label page for a location |

---

## 6. Screen Inventory

| Screen | Description |
|--------|-------------|
| **Locations list** | All active locations; create new; link to detail. |
| **Location detail** | Location info, assigned boxes, archive button, print label. |
| **Boxes list** | All boxes with filter by location / unassigned; create new. |
| **Box detail** | Box info, location assignment, item list with thumbnails, add item, print label. |
| **Items screen** | All items across all boxes; live search; each result links to its box. |
| **Print: box label** | Print-optimised page: Box ID, descriptive label, QR code. |
| **Print: location label** | Print-optimised page: Location code, name, QR code. |

---

## 7. Coding Standards

### 7.1 Explicit Type Annotations

**All** F# bindings must carry explicit type annotations. This applies to:

- Function parameters and return types
- `let` bindings at module scope
- `let` bindings inside functions where the type is not immediately obvious from a literal
- Record field definitions (already typed by the record declaration)
- Discriminated union cases carrying data

The goal is to make the code self-documenting, to surface type errors at the definition
site rather than at the call site, and to make the intent clear to reviewers who may not
have the compiler open.

```fsharp
// ✅ Correct — all parameters and return type annotated
let create (raw: string) : Result<LocationCode, string> =
    ...

// ✅ Correct — local binding annotated where type is non-obvious
let trimmed : string = raw.Trim()

// ✅ Correct — pipeline result annotated at the binding
let codes : LocationCode list =
    rawCodes |> List.choose (LocationCode.create >> Result.toOption)

// ❌ Wrong — return type omitted
let create (raw: string) =
    ...

// ❌ Wrong — parameter type omitted (even when inferable)
let value code =
    let (LocationCode s) = code
    s
```

This rule applies to all projects in the solution: `Shared`, `Server`, `Client`, and
`*.Tests`.

### 7.2 DaisyUI Component Usage

The client uses **DaisyUI** components via Tailwind CSS utility classes. DaisyUI provides
semantic component classes (`btn`, `card`, `badge`, `input`, `modal`, etc.) that map to
a consistent design system without bespoke CSS.

Guidelines:

- Prefer DaisyUI semantic classes over raw Tailwind utilities where a component exists.
- Use DaisyUI theme tokens (`primary`, `secondary`, `accent`, `neutral`, `base-100`,
  `error`, `warning`, `success`, `info`) for colours — never hardcode hex values.
- Use DaisyUI `size` variants (`btn-sm`, `input-lg`, etc.) for consistent sizing.
- Custom CSS is a last resort; document why a DaisyUI component could not be used.

```fsharp
// Example Feliz + DaisyUI button
Html.button [
    prop.className "btn btn-primary btn-sm"
    prop.onClick (fun _ -> dispatch CreateBox)
    prop.text "New Box"
]
```

---

## 8. Testing Strategy

### 8.1 Principles

- **TDD**: tests are written before production code for all domain logic and API handlers.
- **Property testing** (FsCheck): generators cover all domain types. Properties test
  round-trip parsing, smart constructor invariants, and business rule guards.
- **Unit tests**: cover smart constructor edge cases explicitly (empty strings, max length,
  invalid characters, boundary values).
- **Integration tests**: Saturn handler pipeline tested against a temp-file SQLite
  database, including FTS5 search ranking and photo file cleanup on delete.

### 8.2 Example Properties

```fsharp
// Any string that LocationCode.create accepts must round-trip through
// LocationCode.value and be re-parseable to the same value.
let ``location code round-trips through string`` (code: LocationCode) =
    let s = LocationCode.value code
    LocationCode.create s = Ok code

// A box with no assigned location cannot cause an archive failure
// when the archive guard checks its assignment.
let ``unassigned box does not block location archive`` (loc: Location) =
    let result = Location.tryMakeEmpty loc 0
    result |> Result.isOk

// Deleting an item via the API must leave no orphaned photo files on disk.
let ``item deletion removes associated photo file`` (item: Item) =
    // (integration property: filesystem state checked after DELETE call)
    ...
```

---

## 9. Non-Functional Requirements

| ID   | Requirement |
|------|-------------|
| NF-01 | Runs on a single local machine (laptop). No cloud dependency. |
| NF-02 | Browser UI is usable on a mobile phone (responsive layout). |
| NF-03 | Photo upload works from a phone camera (`<input type="file" accept="image/*" capture>`). |
| NF-04 | Starts with a single `dotnet run` command from the repo root. |
| NF-05 | No authentication required (single-user, local network). |
| NF-06 | SQLite path and photo directory are configurable via environment variable, defaulting to `./data/`. |

---

## 10. Open Questions

No open questions at this time.

---

## 11. Glossary

| Term | Definition |
|------|------------|
| Box | A physical cardboard box being packed and tracked. |
| Location | A named, labelled zone within the garage. |
| Item | A named thing recorded as being inside a box, with an optional photo. |
| Assignment | The relationship between a Box and its current Location (or Unassigned). |
| LocationCode | A short uppercase identifier for a Location (e.g. `BACK-LEFT`). |
| BoxId | A sequential human-readable identifier for a Box (e.g. `BOX-042`). |
| PhotoPath | A relative filesystem path to a stored photo file. |
| Label | A printable page with a QR code and identifier for a Box or Location. |

---

*End of v0.2.*
