# Design System

## Overview

A unified design system ensuring consistent spacing, typography, button sizing, and layout across the BoxTracker UI. Built on Tailwind CSS and DaisyUI with a soft, retro "cassette" aesthetic.

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
- All text: `VT323`, `IBM Plex Mono`, fallback to monospace
- No serif or sans-serif fonts

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

- Primary: Dusty rose (`oklch(0.62 0.14 340)`)
- Secondary: Soft sage (`oklch(0.58 0.09 165)`)
- Accent: Soft lavender (`oklch(0.66 0.13 285)`)
- Background: Soft cream (`oklch(0.97 0.008 60)`)
- All colors via DaisyUI semantic classes; never hardcode hex/oklch values
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

