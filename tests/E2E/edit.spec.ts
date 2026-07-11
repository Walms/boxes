import { test, expect } from "@playwright/test";
import {
    alnumToken,
    createLocation,
    createBox,
    addItemToOpenBox,
    openItemDetail,
} from "./helpers";

// Rename/edit flows on the detail pages. Each entity type has an inline edit
// mode reached from its Actions menu; saving PUTs to the server and the
// heading re-renders from the response. These run desktop-only like the other
// flow specs — the edit UI is identical across viewports.

test.beforeEach(({}, testInfo) => {
    test.skip(testInfo.project.name !== "desktop", "edit coverage runs desktop-only");
});

test("rename a box via Edit Label", async ({ page }) => {
    const tok = alnumToken();
    const oldLabel = `Old ${tok}`;
    const newLabel = `New ${tok}`;

    await createBox(page, oldLabel);

    await page.getByRole("button", { name: /Actions/ }).click();
    await page.getByText("Edit Label", { exact: true }).click();
    // The inline edit input is the only visible textbox on the detail page
    // (the add-item form is collapsed), prefilled with the current label.
    const input = page.getByRole("textbox");
    await expect(input).toHaveValue(oldLabel);
    await input.fill(newLabel);
    await page.getByRole("button", { name: "Save", exact: true }).click();

    await expect(page.getByRole("heading", { name: newLabel })).toBeVisible();

    // The Boxes list reflects the rename too.
    await page.goto("/#/boxes");
    await expect(
        page.locator(".catalog-row.entity-box", { hasText: newLabel })
    ).toBeVisible();
    await expect(
        page.locator(".catalog-row.entity-box", { hasText: oldLabel })
    ).toHaveCount(0);
});

test("rename an item via Edit Name", async ({ page }) => {
    const tok = alnumToken();
    const oldName = `Before ${tok}`;
    const newName = `After ${tok}`;

    await createBox(page, `Box ${tok}`);
    await addItemToOpenBox(page, oldName);
    await openItemDetail(page, oldName);

    await page.getByRole("button", { name: /Actions/ }).click();
    await page.getByText("Edit Name", { exact: true }).click();
    const input = page.getByRole("textbox");
    await expect(input).toHaveValue(oldName);
    await input.fill(newName);
    await page.getByRole("button", { name: "Save", exact: true }).click();

    await expect(page.getByRole("heading", { name: newName })).toBeVisible();

    // The Items list reflects the rename too.
    await page.goto("/#/items");
    await expect(
        page.locator(".catalog-row.entity-item", { hasText: newName })
    ).toBeVisible();
    await expect(
        page.locator(".catalog-row.entity-item", { hasText: oldName })
    ).toHaveCount(0);
});

test("rename a location via Edit Name", async ({ page }) => {
    const tok = alnumToken();
    const code = `E${tok}`.slice(0, 12).toUpperCase();
    const oldName = `Attic ${tok}`;
    const newName = `Loft ${tok}`;

    await createLocation(page, code, oldName);
    await page.goto(`/#/locations/${code}`);

    await page.getByRole("button", { name: /Actions/ }).click();
    await page.getByText("Edit Name", { exact: true }).click();
    const input = page.getByRole("textbox");
    await expect(input).toHaveValue(oldName);
    await input.fill(newName);
    await page.getByRole("button", { name: "Save", exact: true }).click();

    await expect(page.getByRole("heading", { name: new RegExp(newName) })).toBeVisible();
});

test("edit a location's code navigates to the new code", async ({ page }) => {
    const tok = alnumToken();
    const oldCode = `C${tok}`.slice(0, 12).toUpperCase();
    const newCode = `D${tok}`.slice(0, 12).toUpperCase();
    const name = `Cellar ${tok}`;

    await createLocation(page, oldCode, name);
    await page.goto(`/#/locations/${oldCode}`);
    await expect(page.getByText(oldCode, { exact: true })).toBeVisible();

    await page.getByRole("button", { name: /Actions/ }).click();
    await page.getByText("Edit Code", { exact: true }).click();
    const input = page.getByRole("textbox");
    await expect(input).toHaveValue(oldCode);
    await input.fill(newCode);
    await page.getByRole("button", { name: "Save", exact: true }).click();

    // A successful code change re-navigates to the detail route for the new
    // code, and the header badge shows it.
    await expect(page).toHaveURL(new RegExp(`#/locations/${newCode}$`));
    await expect(page.getByText(newCode, { exact: true })).toBeVisible();
    await expect(page.getByRole("heading", { name: new RegExp(name) })).toBeVisible();
});

test("archive a location: dropped from list and assign dropdown", async ({ page }) => {
    const tok = alnumToken();
    const code = `A${tok}`.slice(0, 12).toUpperCase();
    const name = `Shed ${tok}`;
    const liveCode = `B${tok}`.slice(0, 12).toUpperCase();
    const liveName = `Barn ${tok}`;

    await createLocation(page, code, name);
    await createLocation(page, liveCode, liveName);
    await page.goto(`/#/locations/${code}`);
    await page.getByRole("button", { name: /Actions/ }).click();
    await page.getByText("Archive", { exact: true }).click();

    // Archiving navigates back to the list. GET /api/locations excludes
    // archived locations by default, so the row disappears while the live
    // location created alongside it stays.
    await expect(page).toHaveURL(/#\/locations$/);
    await expect(
        page.locator(".catalog-row.entity-location", { hasText: liveName })
    ).toBeVisible();
    await expect(
        page.locator(".catalog-row.entity-location", { hasText: name })
    ).toHaveCount(0);

    // An archived location must not be offered as a move target on the box
    // detail page's "Assign to location" dropdown — while a live location
    // created at the same time is, proving the list has actually loaded.
    await createBox(page, `Box ${tok}`);
    await page.getByRole("button", { name: /Unassigned/ }).click();
    const menu = page.locator(".dropdown-content", { hasText: "Unassigned" });
    await expect(menu.getByText(liveName, { exact: true })).toBeVisible();
    await expect(menu.getByText(name, { exact: true })).toHaveCount(0);
});
