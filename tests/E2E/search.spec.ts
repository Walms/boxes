import { test, expect } from "@playwright/test";
import {
    alnumToken,
    createLocation,
    createBox,
    addItemToOpenBox,
} from "./helpers";

// FTS search on the Items page. The query is typed into the search box, debounced,
// and sent to `GET /api/items?q=...` which runs an FTS5 `MATCH`. These tests guard
// both that search finds real items (with denormalized box/location) and that
// user input that isn't a valid FTS expression degrades gracefully instead of
// surfacing a server error.

test.beforeEach(({}, testInfo) => {
    test.skip(testInfo.project.name !== "desktop", "search coverage runs desktop-only");
});

test("search finds an item and shows its box and location", async ({ page }) => {
    const tok = alnumToken();
    const locCode = `S${tok}`.slice(0, 12);
    const locName = `Spot ${tok}`;
    const boxLabel = `Crate ${tok}`;
    const itemWord = alnumToken("find"); // single searchable token, no spaces
    const itemName = `Trinket ${itemWord}`;

    await createLocation(page, locCode, locName);
    const boxId = await createBox(page, boxLabel);

    // Assign the box to the location so the search result can denormalize it.
    await page.goto(`/#/locations/${locCode.toUpperCase()}`);
    await page.getByRole("button", { name: "+ Add Box" }).click();
    let modal = page.locator(".modal-open");
    await modal.getByText(boxLabel, { exact: true }).click();
    await modal.getByRole("button", { name: "Add to Location" }).click();
    await expect(
        page.locator(".catalog-row.entity-box", { hasText: boxLabel })
    ).toBeVisible();

    await page.goto(`/#/boxes/${boxId}`);
    await addItemToOpenBox(page, itemName);

    // Search for the item by its unique token.
    await page.goto("/#/items");
    await page.getByPlaceholder("Search items...").fill(itemWord);

    const card = page.locator(".catalog-row.entity-item", { hasText: itemName });
    await expect(card).toBeVisible();
    await expect(card.getByText(new RegExp(boxLabel))).toBeVisible();
    await expect(card.getByText(new RegExp(locName))).toBeVisible();
    await expect(page.locator(".alert-error")).toHaveCount(0);
});

test("search with FTS special characters does not surface a server error", async ({ page }) => {
    // A bare double-quote / parenthesis is not a valid FTS5 expression. Typing
    // one while searching must not blow up into a server error banner.
    await page.goto("/#/items");
    const box = page.getByPlaceholder("Search items...");

    for (const q of ['"', 'a"b', "foo(", "bar)", "a AND", "*"]) {
        await box.fill(q);
        // Give the debounce + request time to complete, then assert no error.
        await page.waitForTimeout(600);
        await expect(
            page.locator(".alert-error"),
            `query ${JSON.stringify(q)} surfaced an error`
        ).toHaveCount(0);
    }
});

test("search with a multi-word query returns matching items", async ({ page }) => {
    // Multi-word queries contain a space; the query must be encoded and tokenized
    // so both words participate rather than being truncated at the space.
    const tok = alnumToken();
    const first = alnumToken("red");
    const second = alnumToken("ball");
    const itemName = `${first} ${second}`;

    const boxId = await createBox(page, `Box ${tok}`);
    await page.goto(`/#/boxes/${boxId}`);
    await addItemToOpenBox(page, itemName);

    await page.goto("/#/items");
    await page.getByPlaceholder("Search items...").fill(`${first} ${second}`);

    await expect(
        page.locator(".catalog-row.entity-item", { hasText: itemName })
    ).toBeVisible();
    await expect(page.locator(".alert-error")).toHaveCount(0);
});
