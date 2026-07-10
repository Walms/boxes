import { test, expect } from "@playwright/test";

// Navbar navigation, split by viewport: the desktop menu is hidden below `md`,
// and the hamburger dropdown is used on mobile. Each test skips on the project
// where its UI isn't exercised. The nav items are `<a>` elements without an
// `href` (they navigate via Elmish dispatch), so they aren't exposed as the
// `link` role — select them by text, scoped to the right menu container.

test.describe("desktop navbar", () => {
    test.skip(
        ({}, testInfo) => testInfo.project.name !== "desktop",
        "desktop menu only"
    );

    test("navbar links navigate between pages", async ({ page }) => {
        await page.goto("/");
        const nav = page.getByTestId("desktop-nav");

        await nav.getByText("Locations", { exact: true }).click();
        await expect(
            page.getByRole("heading", { name: /^Locations \[/ })
        ).toBeVisible();
        expect(page.url()).toContain("#/locations");

        await nav.getByText("Items", { exact: true }).click();
        await expect(
            page.getByRole("heading", { name: /^Items \[/ })
        ).toBeVisible();
        expect(page.url()).toContain("#/items");

        await nav.getByText("Boxes", { exact: true }).click();
        await expect(
            page.getByRole("heading", { name: /^Boxes \[/ })
        ).toBeVisible();
        expect(page.url()).toContain("#/boxes");
    });
});

test.describe("mobile navbar", () => {
    test.skip(
        ({}, testInfo) => testInfo.project.name !== "mobile",
        "mobile hamburger only"
    );

    test("hamburger dropdown opens and navigates", async ({ page }) => {
        await page.goto("/");

        await page.getByTestId("mobile-nav-toggle").click();
        const menu = page.getByTestId("mobile-nav");

        await menu.getByText("Locations", { exact: true }).click();
        await expect(
            page.getByRole("heading", { name: /^Locations \[/ })
        ).toBeVisible();
        expect(page.url()).toContain("#/locations");
    });
});
