# Design System

## Overview

A unified design system ensuring consistent spacing, typography, button sizing, and layout across the BoxTracker UI. Built on Tailwind CSS and DaisyUI with a **modern-retro SNES** aesthetic — a muted 16-bit "application chrome" palette (modelled on SNES non-game software like Mario Paint / BS-X) softened with a readable retro font, gentle rounded corners, soft shadows, and a subtle CRT scanline/vignette overlay. The theme is defined as the DaisyUI theme `nintendo` in `src/Client/app.css` and applied via `data-theme="nintendo"` on `<html>`.

### Retro treatment (in `app.css`)

- **CRT overlay**: fixed `body::before` scanlines + `body::after` tube vignette, layered above content (`z-index` 9998–9999) but `pointer-events: none`. The full-screen image viewer sits above them (`z-index: 10000`). Scanlines are deliberately whisper-faint — a hairline every 4px at `rgba(42,47,62,0.025)` so they read as texture, not stripes; the vignette fades in from 68% at `0.05`.
- **Accessibility / preference media queries**: `prefers-reduced-motion: reduce` disables `scroll-behavior: smooth` and removes button-lift transforms + colour/opacity transitions; `prefers-contrast: more` hides both CRT overlays so nothing dims content.
- **Focus & selection**: keyboard focus shows a 2px faded-gold (`--color-accent`) outline via `:focus-visible` on links/buttons/inputs (mouse/touch unaffected); `::selection` uses brick-red primary instead of browser-default blue.
- **Borders**: panels (`.card`, `.modal-box`, dropdowns, alerts), buttons, and inputs carry a 2px ink outline (`--nin-ink: #2a2f3e`).
- **Corners**: gently rounded — `--radius-field`/`--radius-selector` `0.375rem`, `--radius-box` `0.5rem` (softened from the earlier hard `0`).
- **Shadows**: soft, modern (`--nin-shadow: 0 2px 6px rgba(42,47,62,0.16)`, plus `-sm`/`-lg` variants). Buttons lift gently on hover and settle on active; transitions are smooth `ease` (~120–150ms), not stepped.
- **Navbar**: bottom ink border capped by a thin brick-red (`--color-primary`) accent strip.
- **Entity colour-coding**: list rows get a 5px coloured left-border + faint tint — locations indigo `#7a74b5`, boxes sage `#639b82`, items brick `#c05850` (`.entity-location` / `.entity-box` / `.entity-item`).

## Spacing Scale

All spacing uses Tailwind's default spacing scale, grouped by semantic purpose:

| Purpose | Mobile | Desktop | Tailwind Class |
|---------|--------|---------|-----------------|
| Outer padding | `px-3` | `px-4 md:px-6` | `px-3 sm:px-4 md:px-6` |
| Container vertical | `py-3` | `py-4` | `py-3 sm:py-4` |
| Section margin | `mb-4` | `mb-6` | `mb-4 sm:mb-6` |
| Component gap | `gap-3` | `gap-4` | `gap-3 sm:gap-4` |
| Card padding | `p-4` | `p-6` | `p-4 sm:p-6` |
| Item spacing | `space-y-3` | - | `space-y-3` |
| Form field spacing | `mb-4` | - | `mb-4` |
| Modal spacing | `mb-4` | - | `mb-4` |
| Tight spacing | `gap-1 sm:gap-2` | - | `gap-1 sm:gap-2` |

**Rule**: No hardcoded spacing values. Use only Tailwind spacing utilities. For tighter control, use responsive prefixes (e.g., `gap-3 sm:gap-4`).

## Typography

### Font Family
- All text: `VT323` (a readable retro terminal/pixel font), falling back to `ui-monospace` and other monospace families
- Loaded lazily from Google Fonts in `src/Client/index.html` (`media="print"` + `onload` swap, with a `<noscript>` fallback)
- No serif or sans-serif fonts
- VT323 renders compact, so the type scale (below) runs a touch larger than a default sans scale

### Type scale remap

Every Tailwind text utility (`text-xs` … `text-3xl`) is remapped in the `@theme` block of `app.css` onto a single VT323-tuned scale, so the many size classes scattered through the UI resolve consistently:

| Utility | Size |
|---------|------|
| `text-xs` / `text-sm` | 0.85 / 0.95rem |
| `text-base` | 1.05rem |
| `text-lg` / `text-xl` | 1.3 / 1.45rem |
| `text-2xl` / `text-3xl` | 1.85 / 2.2rem |

Headings reuse this scale (`h1`→`text-2xl`, `h2`→`text-lg`, `h3`→`text-base`) with no bespoke sizes and no pixel text-shadow.

### Size Hierarchy

| Element | Size | Responsive | Class |
|---------|------|-----------|--------|
| Page heading (H1) | 28px (text-2xl) | 36px (sm:text-3xl) | `text-2xl sm:text-3xl` |
| Section heading (H2) | 20px (text-lg) | 24px (sm:text-2xl) | `text-lg sm:text-2xl` |
| Subsection heading (H3) | 18px (text-lg) | - | `text-lg` |
| Card title | 18px (text-lg) | - | `text-lg` |
| Body text | 16px (text-base) | - | `text-base` |
| Secondary text | 14px (text-sm) | - | `text-sm` |
| Small labels | 12px (text-xs) | 14px (sm:text-sm) | `text-xs sm:text-sm` |
| Emphasis | - | - | `font-bold`, `font-semibold` |

**Rule**: Page headings are always `text-2xl sm:text-3xl`. Section headings use `text-lg sm:text-2xl`. All form labels use `text-sm font-medium`.

### Text Styling
- Labels: `text-sm font-medium`
- Button text: Default DaisyUI, no custom sizing
- Links: Use semantic button classes (avoid bare `<a>` tags for navigation)
- Secondary text: Use `opacity-60` or `opacity-70` instead of lighter colors

## Buttons

### Size Standards

All buttons use DaisyUI semantic classes. No custom sizing.

| Context | Size | Mobile Width | Class |
|---------|------|--------------|--------|
| Primary actions (create, save) | `btn-md` | Full width | `btn btn-primary` / `btn btn-primary w-full sm:w-auto` |
| Primary (small context) | `btn-sm` | Full width in modals | `btn btn-primary btn-sm` |
| Secondary actions (edit, cancel) | `btn-sm` | Full width | `btn btn-ghost btn-sm` / `btn btn-outline btn-sm` |
| Icon buttons (circular) | `btn-circle` | Sizes vary | `btn btn-ghost btn-circle btn-lg` (mobile) / `btn btn-ghost btn-circle btn-md` (desktop) |
| Destructive (delete, archive) | `btn-sm` | Full width | `btn btn-error btn-sm` |
| Disabled state | Same size | - | Use native `disabled` attribute |

**Rule**: 
- Page-level create buttons: `btn btn-primary btn-sm sm:btn-md w-full sm:w-auto`
- Modal action buttons: `btn btn-primary btn-sm` (always small)
- Buttons in lists: `btn btn-ghost btn-sm`
- Icon buttons in navbar: `btn btn-ghost btn-circle btn-lg md:btn-md`
- Never use `btn-lg` in action lists

### Button Layout
- Horizontal button groups: `flex gap-2 justify-end` (desktop) or `flex gap-1 w-full sm:w-auto` (mobile)
- Full-width on mobile: Use `w-full sm:w-auto` on button containers
- Flex-based: `flex-1` to distribute equally; auto width for specific button groups
- Wrapping: Allow buttons to wrap with `flex-wrap` on mobile for many-button scenarios

**Rule**: Modal buttons always use `btn-ghost` + `btn-sm`. Never full-width inside modals. List action buttons use `flex gap-1` with `flex-1` for distribution.

## Cards & Containers

### Card Styling
All `.card` elements have:
- Border: `border border-base-300`
- Padding: `p-4 sm:p-6`
- Shadow: Default DaisyUI (`box-shadow: 0 2px 8px ...`)
- Background: `bg-base-200`
- Hover: `hover:bg-base-300 transition-colors` (clickable cards only)

**Rule**: Forms and detail panels are always `.card` with border. Grid items may omit border if part of a larger grouped layout.

### Grid Layouts

| Layout | Columns | Gap | Breakpoint |
|--------|---------|-----|------------|
| Location cards | 1 | `gap-3` | Full width |
| Location cards | 2 | `gap-3` | `sm:` (384px+) |
| Location cards | 3 | `gap-4` | `lg:` (1024px+) |
| **Grid class** | - | - | `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-4` |

**Rule**: Never use `md:grid-cols-2`. Always start from mobile single-column, then `sm:grid-cols-2`, then `lg:grid-cols-3`.

## Forms

### Input Fields
- Class: `input input-bordered`
- Padding: Inherited from DaisyUI
- Focus state: `focus:input-primary` (amber glow)
- Text size: Always `text-base` for mobile touch comfort
- Full width: On mobile; `flex-1` in flex contexts

### Select/Dropdown
- Class: `select select-bordered`
- Focus state: `focus:select-primary`
- Text size: `text-base`

### Labels
- Class: `label`
- Label text: `label-text text-sm font-medium`
- Spacing below: `pb-3` (provides comfortable spacing to input)
- Container: Wrap in `.form-control` with `mb-4` gap from next field

### File Input
- Class: `file-input file-input-bordered text-base`
- Use `text-base` for proper sizing on mobile

**Rule**: All form inputs use `text-base` to ensure 16px min (prevents iOS zoom on focus). Labels always use `text-sm font-medium pb-2`.

## Lists & Items

### List Containers
- Class: `space-y-3` for default spacing between items
- Class: `space-y-2` for tighter spacing
- Never use `gap-*` on `<ul>`; use `space-y-*` instead

### List Items (Cards)
Responsive layout for item rows:
- Mobile: `flex-col gap-3`
- Desktop: `sm:flex-row sm:items-center`
- Full container class: `flex flex-col sm:flex-row sm:items-center gap-3 p-3 sm:p-4 rounded-lg`

### Photos in Lists
- Small thumbnails: `w-10 h-10` (in dialogs)
- Item thumbnails: `w-12 h-12 sm:w-14 sm:h-14` (in lists)
- Search results: `w-14 h-14 sm:w-16 sm:h-16` (larger results)
- Detail photos: `w-48 h-48` (box/item detail pages)
- Always use `rounded` and `object-cover`

**Rule**: All photos must be clickable with `cursor-pointer hover:opacity-80 transition-opacity`.

## Modals

### Modal Box
- Width: `w-11/12 max-w-md sm:max-w-lg`
- Never full width; always constrained
- Padding: DaisyUI default (inherited from `.modal-box`)

### Modal Content
- Title: `h3` with `font-bold text-lg mb-4`
- Body spacing: `mb-4` between sections
- Action buttons: Always `btn-sm` (no variations)
- Layout: `modal-action` flex row, `justify-end` on desktop

**Rule**: Modals never have full-width buttons. Titles use `h3` with `font-bold text-lg mb-4`.

## Loading & Async Feedback

Every async operation surfaces a loading indicator so the UI is never silently blank or showing a premature empty state.

| Context | Indicator | Driven by |
|---------|-----------|-----------|
| Initial list load (Locations, Boxes, Items) | Full-width `loading-spinner loading-lg` spanning the grid | `state.Loading` while the collection is empty |
| Detail page load (Location, Box, Item) | Centred `loading-spinner loading-lg` | `state.Loading` while the detail is `None` |
| Item search (debounced) | `loading-spinner loading-sm` overlaid in the search input; large spinner replaces an empty list | `state.SearchLoading` |
| Modal data loads (move item, add existing item, add box to location) | `loading-spinner loading-md` inside the modal body | `state.DialogLoading` |
| Photo upload / background processing | `photoStatusBanner` (spinner + status text) | `state.UploadingPhoto`, `state.PhotoProcessing` |
| History modal | `loading-spinner loading-md` | `state.HistoryLoading` |

Shared helpers in `Pages/Common.fs`: `loadingSpinner` (centred, gated on `state.Loading`), `gridLoadingSpinner` (`col-span-full`, for grids), `dialogLoadingSpinner` (for modal bodies).

**Rule**: `DialogLoading` is shared across modals (only one modal is open at a time) and is set `true` when a `Show*Dialog` message fires a fetch, cleared when its `*Loaded` message resolves. An empty-state message ("No boxes available", etc.) must only render when its loading flag is `false`, so it never flashes before data arrives.

## Empty States

All empty state text:
- Container: `text-center py-8 sm:py-12 opacity-60`
- Heading: `text-lg`
- Secondary: `text-sm mt-2`

**Rule**: Empty states always use `opacity-60` and center-align with generous vertical padding.

## Responsive Breakpoints (Tailwind Defaults)

| Name | Min Width | Use |
|------|-----------|-----|
| Mobile (default) | 0 | No prefix |
| Small (`sm:`) | 384px | Tablets, landscape phones |
| Medium (`md:`) | 768px | Tablets, small laptops |
| Large (`lg:`) | 1024px | Desktops |

**Rule**: Start mobile-first (no prefix), then add breakpoints: `sm:`, `lg:`. Rarely use `md:` unless specific layout needs it.

## Color & Theme

The `nintendo` theme uses a muted 16-bit slate palette (defined once in `app.css`):

- Base: slate-grey desktop `#b8c0d2` (`base-100`), light panels `#eceff6` (`base-200`), mid-grey borders `#ced5e2` (`base-300`), soft dark slate ink `#2a2f3e` (`base-content`)
- Primary: dusty brick red `#c05850`
- Secondary: muted sage / pipe green `#639b82`
- Accent: faded gold `#d4b254`
- Neutral: slate `#434860`
- Status colours reuse the palette: `error`→brick, `success`→sage, `warning`→gold, `info`→`#6685b0`
- All colours consumed via DaisyUI semantic classes; never hardcode hex values in components
- Never use `bg-white`, `text-black` — use `base-100`, `base-content` instead

**Rule**: Use only DaisyUI color semantics. No custom colors. No opacity overrides except for secondary text (`opacity-60`, `opacity-70`).

## Implementation Checklist

When updating any page:
1. ✓ Page heading: `text-2xl sm:text-3xl font-bold`
2. ✓ Section headings: `text-lg sm:text-2xl font-bold` (or `font-semibold`)
3. ✓ All buttons: Use semantic DaisyUI classes (no custom `btn-*` values)
4. ✓ Form inputs: `text-base`, labels use `text-sm font-medium pb-2`
5. ✓ Cards: Border, padding, shadow via `.card` class
6. ✓ Grids: `grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-4`
7. ✓ Spacing: Only Tailwind utilities, no hardcoded values
8. ✓ Colors: DaisyUI semantic only; no hardcoded colors
9. ✓ Mobile width: All buttons and inputs full width on mobile (`w-full sm:w-auto`)
10. ✓ Typography: Consistent font sizes per hierarchy chart

