import { test, expect } from "@playwright/test";
import {
    trackConsoleErrors,
    alnumToken,
    createLocation,
    createBox,
    addItemToOpenBox,
} from "./helpers";

// Tier 2 — core domain flows. These exercise the event-sourced move model
// end-to-end through the real UI, server, and SQLite. Each test creates its
// own uniquely-named entities and asserts only on those, so leftovers from
// other tests can't interfere. Desktop only: they drive detail pages and
// modals, not the responsive nav, which the smoke/nav specs already cover.

test.beforeEach(({}, testInfo) => {
    test.skip(testInfo.project.name !== "desktop", "flow coverage runs desktop-only");
});

test("box lifecycle: create, assign to location, add item", async ({ page }, testInfo) => {
    const errors = trackConsoleErrors(page);
    const tok = alnumToken();
    const locCode = `L${tok}`.slice(0, 12);
    const locName = `Loc ${tok}`;
    const boxLabel = `Box ${tok}`;
    const itemName = `Lamp ${tok}`;

    await createLocation(page, locCode, locName);
    const boxId = await createBox(page, boxLabel);

    // Assign the box to the location from the location detail page's
    // "+ Add Box" modal (a plain list, not a focus-only dropdown).
    await page.goto(`/#/locations/${locCode.toUpperCase()}`);
    await page.getByRole("button", { name: "+ Add Box" }).click();
    const modal = page.locator(".modal-open");
    await modal.getByText(boxLabel, { exact: true }).click();
    await modal.getByRole("button", { name: "Add to Location" }).click();

    // The box now shows in the location's box list.
    await expect(
        page.locator(".catalog-row.entity-box", { hasText: boxLabel })
    ).toBeVisible();

    // And the box detail page reflects the location assignment.
    await page.goto(`/#/boxes/${boxId}`);
    await expect(
        page.getByText(locName, { exact: true }).first()
    ).toBeVisible();

    // Add an item to the box; it appears in the box's item list...
    await addItemToOpenBox(page, itemName);

    // ...and in the global Items list.
    await page.goto("/#/items");
    await expect(
        page.locator(".catalog-row.entity-item", { hasText: itemName })
    ).toBeVisible();

    expect(errors, `unexpected page errors:\n${errors.join("\n")}`).toEqual([]);
});

test("deleting a box preserves its items as unassigned", async ({ page }, testInfo) => {
    // The key event-sourcing invariant: deleting a box unassigns its items
    // rather than destroying them, so item↔history associations survive.
    const tok = alnumToken();
    const boxLabel = `Box ${tok}`;
    const itemName = `Widget ${tok}`;

    const boxId = await createBox(page, boxLabel);
    await addItemToOpenBox(page, itemName);

    // Delete the box via the Actions menu on the box detail page.
    await page.getByRole("button", { name: /Actions/ }).click();
    await page.getByText("Delete Box", { exact: true }).click();
    await expect(page).toHaveURL(/#\/boxes$/);

    // The item survives on the Items page, shown as Unassigned.
    await page.goto("/#/items");
    const card = page.locator(".catalog-row.entity-item", { hasText: itemName });
    await expect(card).toBeVisible();
    await expect(card.getByText("Unassigned", { exact: true })).toBeVisible();
});

test("move an item from one box to another", async ({ page }, testInfo) => {
    const tok = alnumToken();
    const itemName = `Gadget ${tok}`;
    const boxA = await createBox(page, `BoxA ${tok}`);
    const boxB = await createBox(page, `BoxB ${tok}`);

    // Add the item to box A.
    await page.goto(`/#/boxes/${boxA}`);
    await addItemToOpenBox(page, itemName);

    // Open the item's ⋮ menu and move it to box B.
    const itemRow = page.locator(".catalog-row", { hasText: itemName });
    await itemRow.getByRole("button", { name: "⋮" }).click();
    await page.getByText("Move to box", { exact: true }).click();
    const modal = page.locator(".modal-open");
    await modal.getByText(`BoxB ${tok}`, { exact: true }).click();
    await modal.getByRole("button", { name: "Move", exact: true }).click();

    // Box A no longer lists the item.
    await expect(
        page.locator(".catalog-row", { hasText: itemName })
    ).toHaveCount(0);

    // Box B now lists the item.
    await page.goto(`/#/boxes/${boxB}`);
    await expect(
        page.locator(".catalog-row", { hasText: itemName })
    ).toBeVisible();
});

test("unassign an item from its box", async ({ page }, testInfo) => {
    const tok = alnumToken();
    const itemName = `Doohickey ${tok}`;
    const boxId = await createBox(page, `Box ${tok}`);
    await page.goto(`/#/boxes/${boxId}`);
    await addItemToOpenBox(page, itemName);

    const itemRow = page.locator(".catalog-row", { hasText: itemName });
    await itemRow.getByRole("button", { name: "⋮" }).click();
    await page.getByText("Unassign", { exact: true }).click();

    // Item leaves the box list.
    await expect(
        page.locator(".catalog-row", { hasText: itemName })
    ).toHaveCount(0);

    // And shows as Unassigned on the Items page.
    await page.goto("/#/items");
    const card = page.locator(".catalog-row.entity-item", { hasText: itemName });
    await expect(card).toBeVisible();
    await expect(card.getByText("Unassigned", { exact: true })).toBeVisible();
});
