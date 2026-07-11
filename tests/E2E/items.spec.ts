import { test, expect } from "@playwright/test";
import {
    alnumToken,
    createBox,
    addItemToOpenBox,
    openItemDetail,
} from "./helpers";

// Standalone item management from the Items page: the "+ New Item" form
// (which can optionally assign straight into a box) and deletion from the
// item detail page. Desktop-only like the other flow specs.

test.beforeEach(({}, testInfo) => {
    test.skip(testInfo.project.name !== "desktop", "item coverage runs desktop-only");
});

test("create a standalone item: starts unassigned", async ({ page }) => {
    const tok = alnumToken();
    const itemName = `Spanner ${tok}`;

    await page.goto("/#/items");
    await page.getByRole("button", { name: "+ New Item" }).click();
    await page.getByPlaceholder("e.g. Kitchen knife set").fill(itemName);
    await page.getByRole("button", { name: "Create", exact: true }).click();

    const card = page.locator(".catalog-row.entity-item", { hasText: itemName });
    await expect(card).toBeVisible();
    await expect(card.getByText("Unassigned", { exact: true })).toBeVisible();
});

test("create an item assigned to a box from the Items page", async ({ page }) => {
    const tok = alnumToken();
    const boxLabel = `Toolbox ${tok}`;
    const itemName = `Pliers ${tok}`;

    const boxId = await createBox(page, boxLabel);

    await page.goto("/#/items");
    await page.getByRole("button", { name: "+ New Item" }).click();
    const form = page.locator(".card", { hasText: "Assign to box" });
    await form.getByPlaceholder("e.g. Kitchen knife set").fill(itemName);
    await form.getByRole("combobox").selectOption({ label: boxLabel });
    await form.getByRole("button", { name: "Create", exact: true }).click();

    // The new item's row shows its box rather than Unassigned.
    const card = page.locator(".catalog-row.entity-item", { hasText: itemName });
    await expect(card).toBeVisible();
    await expect(card.getByText(new RegExp(boxLabel))).toBeVisible();

    // And the box detail page lists it.
    await page.goto(`/#/boxes/${boxId}`);
    await expect(
        page.locator(".catalog-row", { hasText: itemName })
    ).toBeVisible();
});

test("the Create button is disabled until a name is entered", async ({ page }) => {
    await page.goto("/#/items");
    await page.getByRole("button", { name: "+ New Item" }).click();
    const form = page.locator(".card", { hasText: "Assign to box" });
    await expect(form.getByRole("button", { name: "Create", exact: true })).toBeDisabled();
    await form.getByPlaceholder("e.g. Kitchen knife set").fill("x");
    await expect(form.getByRole("button", { name: "Create", exact: true })).toBeEnabled();
});

test("delete an item from its detail page", async ({ page }) => {
    const tok = alnumToken();
    const itemName = `Ephemeral ${tok}`;

    await createBox(page, `Box ${tok}`);
    await addItemToOpenBox(page, itemName);
    await openItemDetail(page, itemName);

    await page.getByRole("button", { name: /Actions/ }).click();
    await page.getByText("Delete Item", { exact: true }).click();

    // Deleting the currently-open item navigates back to the list, where the
    // item is gone for good.
    await expect(page).toHaveURL(/#\/items$/);
    await expect(
        page.locator(".catalog-row.entity-item", { hasText: itemName })
    ).toHaveCount(0);
    await page.reload();
    await expect(page.getByRole("heading", { name: /^Items \[/ })).toBeVisible();
    await expect(
        page.locator(".catalog-row.entity-item", { hasText: itemName })
    ).toHaveCount(0);
});
