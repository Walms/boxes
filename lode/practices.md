## Coding Standards

- All F# bindings must carry explicit type annotations (parameters, return types, non-obvious locals)
- No comments unless explicitly requested
- Parse-don't-validate: all value objects use private constructors with `Result<'T, string>` smart constructors
- DaisyUI semantic classes over raw Tailwind utilities; DaisyUI theme tokens for colours
- No hardcoded hex colours; no custom CSS unless a DaisyUI component doesn't exist

## Architecture Patterns

- Shared project (netstandard2.0) holds all domain types, validation, and smart constructors — no I/O
- Server references Shared; Client references Shared; both compile the same domain code
- Fable 5 compiles Client (netstandard2.0) to JS
- Event-sourced move model: placement derived from append-only `move` table via `ROW_NUMBER()` window function
- Items, boxes, and locations are independent entities — no FK columns for placement
- SQLite FTS5 contentless table (`item_search`) maintained by application layer, deriving box/location from move log
- Photo storage: flat files at `data/photos/{boxId}/{guid}.{ext}`, relative paths in DB

## Fable + JSON Conventions

- Server `System.Text.Json` uses no `CamelCase` naming policy — outputs PascalCase to match Fable record field names (which are preserved as-is in compiled JS)
- `PropertyNameCaseInsensitive = true` so server accepts PascalCase request bodies from Fable client
- Client DTO collection fields must use `array` (not `list`) — `JSON.parse` returns plain JS arrays; Fable's `list` is a linked list with `head`/`tail` properties, so `List.isEmpty` always returns `true` for a plain JS array
- Use `Array.isEmpty` instead of `.IsEmpty` for array checks; `for ... in ...` works for both arrays and lists

## Testing

- xUnit + FsCheck for property tests
- Domain.Tests: unit + property tests against Shared layer
- Api.Tests: integration tests against temp SQLite file
