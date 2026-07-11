# E2E tests (Playwright)

End-to-end tests that drive the **production client bundle** (`vite preview`
over `vite build` output) against the real ASP.NET server and a throwaway
SQLite database. This catches regressions unit tests can't — broken rendering,
routing, Elmish wiring, API/DTO mismatches.

See [../../lode/plans/playwright-e2e-ci.md](../../lode/plans/playwright-e2e-ci.md)
for the full plan and test roadmap.

## Running locally

Playwright's `webServer` boots both processes for you (server on `:5000`,
`vite preview` on `:4173`). You need the client built first:

```bash
nix develop --command bash -c "npm run build && npx playwright test"
```

Each run points `BOXTRACKER_DATA` at `tests/E2E/.data` (gitignored) unless you
override `E2E_DATA_DIR`. To start from an empty database, delete that folder.

Useful flags:

```bash
npx playwright test --project=desktop       # one viewport only
npx playwright test --headed --debug         # step through in a browser
npx playwright show-report                   # open the last HTML report
```

## Layout

- `smoke.spec.ts` — Tier 1: bundle boots, hash routing, reload survival.
- `nav.spec.ts` — navbar navigation (desktop menu + mobile hamburger).
- `helpers.ts` — shared utilities (console-error tracking, unique tags).

## Conventions

- **Single worker, shared DB.** Tests create their own uniquely-named entities
  and never assume an empty list, so leftovers can't break later tests.
- **User-facing selectors** (`getByRole`, `getByText`, `getByLabel`) preferred;
  `data-testid` only where markup is genuinely ambiguous (e.g. the duplicated
  desktop/mobile nav menus).
