import { test, expect } from "@playwright/test";
import { trackConsoleErrors } from "./helpers";

// Tier 1 smoke tests: prove the Fable-compiled bundle loads, renders, and
// routes. These run on both the `desktop` and `mobile` projects. They only
// assert on their own navigation — never on list contents — so leftovers from
// other tests can't break them.

test("app boots: navbar and Boxes landing render with no JS errors", async ({
    page,
}) => {
    const errors = trackConsoleErrors(page);

    await page.goto("/");

    // Brand in the navbar and the Boxes list heading (e.g. "Boxes [000]").
    await expect(page.getByText("BoxTracker™")).toBeVisible();
    await expect(
        page.getByRole("heading", { name: /^Boxes \[/ })
    ).toBeVisible();

    expect(errors, `unexpected page errors:\n${errors.join("\n")}`).toEqual([]);
});

test("hash routing: deep links land on the right page", async ({ page }) => {
    await page.goto("/#/locations");
    await expect(
        page.getByRole("heading", { name: /^Locations \[/ })
    ).toBeVisible();

    await page.goto("/#/items");
    await expect(
        page.getByRole("heading", { name: /^Items \[/ })
    ).toBeVisible();
});

test("hash routing: an unknown hash falls back to Boxes", async ({ page }) => {
    await page.goto("/#/definitely-not-a-real-route");
    await expect(
        page.getByRole("heading", { name: /^Boxes \[/ })
    ).toBeVisible();
});

test("hash routing: a deep-linked route survives a reload", async ({
    page,
}) => {
    await page.goto("/#/items");
    await expect(
        page.getByRole("heading", { name: /^Items \[/ })
    ).toBeVisible();

    await page.reload();
    await expect(
        page.getByRole("heading", { name: /^Items \[/ })
    ).toBeVisible();
    expect(page.url()).toContain("#/items");
});
