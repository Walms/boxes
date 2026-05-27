# Domain Types

Core types live in `src/Shared/Domain/`. Each value object has:
- A private single-case DU constructor
- A `create : string -> Result<'T, string>` smart constructor in a companion module
- A `value : 'T -> string` extractor

## Identifiers
- `LocationCode` — uppercase, letters/digits/hyphens, max 20 chars
- `BoxId` — system-assigned sequential (`BOX-NNN`), not user-created

## Value Objects
- `LocationName` — non-empty, max 200 chars
- `BoxLabel` — optional, non-empty if present, max 200 chars
- `ItemName` — non-empty, max 200 chars
- `PhotoPath` — relative path `photos/{boxId}/{guid}.{ext}`

## Container DU (`Container.fs`)

```fsharp
type Container =
    | Unassigned
    | InBox of BoxId: BoxTracker.BoxId.BoxId
    | AtLocation of LocationCode: BoxTracker.LocationCode.LocationCode
```

Both `Item.Placement` and `Box.Placement` are `Container` values derived from the latest move for that entity. The domain types carry `Placement` fields populated by the storage layer.

## Aggregate Roots
```fsharp
type Location = { Code: LocationCode; Name: LocationName; IsArchived: bool; CreatedAt: DateTimeOffset }
type Box = { Id: BoxId; Label: BoxLabel option; Placement: Container; CreatedAt: DateTimeOffset }
type Item = { Id: Guid; Name: ItemName; Photo: PhotoPath option; Placement: Container; AddedAt: DateTimeOffset }
type Move = { Id: Guid; EntityType: string; EntityId: string; To: Container; MovedAt: DateTimeOffset }
```

## Business Rules
- `EmptyLocation` type proves a Location has no boxes before archiving
- Box IDs are never reused (sequential, no gaps)
- Items are independent entities — deleting a box unassigns its items (creates unassignment moves), does NOT delete them
- Placement is always derived from the move log, never stored as a FK column

See also: [persistence.md](persistence.md) for the SQLite schema, [api.md](api.md) for API routes.
