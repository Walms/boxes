import { test, expect } from "@playwright/test";
import { alnumToken, createLocation, createBox } from "./helpers";

// List filtering. The Boxes and Locations pages have client-side text filters
// over label/name/code; the Boxes page additionally has a location dropdown
// that refetches from the server (GET /api/boxes?location=...). Desktop-only
// like the other flow specs.

test.beforeEach(({}, testInfo) => {
    test.skip(testInfo.project.name !== "desktop", "filter coverage runs desktop-only");
});

test("text filter narrows the box list by label", async ({ page }) => {
    const tok = alnumToken();
    const labelA = `Alpha ${tok}`;
    const labelB = `Beta ${tok}`;
    await createBox(page, labelA);
    await createBox(page, labelB);

    await page.goto("/#/boxes");
    const filter = page.getByPlaceholder("Filter boxes...");

    // Matching is a case-insensitive substring on the label.
    await filter.fill(`alpha ${tok}`);
    await expect(
        page.locator(".catalog-row.entity-box", { hasText: labelA })
    ).toBeVisible();
    await expect(
        page.locator(".catalog-row.entity-box", { hasText: labelB })
    ).toHaveCount(0);

    // A query matching nothing shows the empty-state message.
    await filter.fill(`nothing-matches-${tok}`);
    await expect(page.getByText("No boxes match your search")).toBeVisible();

    // Clearing restores both.
    await filter.fill("");
    await expect(
        page.locator(".catalog-row.entity-box", { hasText: labelA })
    ).toBeVisible();
    await expect(
        page.locator(".catalog-row.entity-box", { hasText: labelB })
    ).toBeVisible();
});

test("location dropdown filters boxes to those at that location", async ({ page }) => {
    const tok = alnumToken();
    const locCode = `F${tok}`.slice(0, 12).toUpperCase();
    const locName = `Basement ${tok}`;
    const assignedLabel = `Stored ${tok}`;
    const strayLabel = `Stray ${tok}`;

    await createLocation(page, locCode, locName);
    await createBox(page, assignedLabel);

    // Assign via the box detail page's "Assign to location" dropdown.
    await page.getByRole("button", { name: /Unassigned/ }).click();
    await page
        .locator(".dropdown-content")
        .getByText(locName, { exact: true })
        .click();
    await expect(page.getByRole("button", { name: new RegExp(locName) })).toBeVisible();

    await createBox(page, strayLabel); // left unassigned

    await page.goto("/#/boxes");
    // The location filter is the select whose first option is "All locations".
    const locFilter = page.locator("select", {
        has: page.locator('option:text-is("All locations")'),
    });
    await locFilter.selectOption({ label: locName });

    await expect(
        page.locator(".catalog-row.entity-box", { hasText: assignedLabel })
    ).toBeVisible();
    await expect(
        page.locator(".catalog-row.entity-box", { hasText: strayLabel })
    ).toHaveCount(0);

    // Back to all locations, the unassigned box reappears.
    await locFilter.selectOption({ label: "All locations" });
    await expect(
        page.locator(".catalog-row.entity-box", { hasText: strayLabel })
    ).toBeVisible();
});

test("text filter narrows the location list by name or code", async ({ page }) => {
    const tok = alnumToken();
    const codeA = `Y${tok}`.slice(0, 12).toUpperCase();
    const codeB = `Z${tok}`.slice(0, 12).toUpperCase();
    const nameA = `Yard ${tok}`;
    const nameB = `Zone ${tok}`;
    await createLocation(page, codeA, nameA);
    await createLocation(page, codeB, nameB);

    await page.goto("/#/locations");
    const filter = page.getByPlaceholder("Filter locations...");

    // Match by name.
    await filter.fill(`yard ${tok}`);
    await expect(
        page.locator(".catalog-row.entity-location", { hasText: nameA })
    ).toBeVisible();
    await expect(
        page.locator(".catalog-row.entity-location", { hasText: nameB })
    ).toHaveCount(0);

    // Match by code (case-insensitive).
    await filter.fill(codeB.toLowerCase());
    await expect(
        page.locator(".catalog-row.entity-location", { hasText: nameB })
    ).toBeVisible();
    await expect(
        page.locator(".catalog-row.entity-location", { hasText: nameA })
    ).toHaveCount(0);

    // No match shows the empty-state message.
    await filter.fill(`nothing-matches-${tok}`);
    await expect(page.getByText("No locations match your search")).toBeVisible();
});
