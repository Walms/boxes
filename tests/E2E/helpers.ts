import { Page, TestInfo, expect } from "@playwright/test";

/**
 * Collects real JavaScript failures from a page: uncaught exceptions
 * (`pageerror`) and `console.error` calls. Browser-level resource load
 * failures for third-party assets (e.g. the Google Fonts stylesheet, which
 * may be blocked in CI) are filtered out so the smoke assertion stays about
 * the app's own code, not the network environment.
 */
export function trackConsoleErrors(page: Page): string[] {
    const errors: string[] = [];
    page.on("pageerror", (err) => errors.push(`pageerror: ${err.message}`));
    page.on("console", (msg) => {
        if (msg.type() !== "error") return;
        const text = msg.text();
        if (/fonts\.(googleapis|gstatic)\.com/.test(text)) return;
        if (/Failed to load resource/.test(text) && /fonts\./.test(text)) return;
        errors.push(`console.error: ${text}`);
    });
    return errors;
}

/** A short, per-test unique tag so entities never collide across tests/retries. */
export function uniqueTag(testInfo: TestInfo): string {
    return `${testInfo.testId}-${Date.now().toString(36)}`;
}

/**
 * A short, alphanumeric-only token (no hyphens/spaces) suitable for embedding
 * in a name that will later be searched — safe for URLs and the FTS tokenizer.
 */
export function alnumToken(prefix = "z"): string {
    return `${prefix}${Math.random().toString(36).slice(2, 9)}`;
}

/** Create a location via the UI and return once it appears in the list. */
export async function createLocation(
    page: Page,
    code: string,
    name: string
): Promise<void> {
    await page.goto("/#/locations");
    await page.getByRole("button", { name: "+ New Location" }).click();
    // Both placeholders share the "e.g. …" prefix and getByPlaceholder matches
    // case-insensitive substrings, so pin them exactly to disambiguate.
    await page.getByPlaceholder("e.g. GARAGE", { exact: true }).fill(code);
    await page.getByPlaceholder("e.g. Garage", { exact: true }).fill(name);
    await page.getByRole("button", { name: "Create", exact: true }).click();
    await expect(page.getByText(name, { exact: true })).toBeVisible();
}

/**
 * Create a box via the UI and return its generated id (BOX-0NN), read from the
 * URL after opening the new box's detail page.
 */
export async function createBox(page: Page, label: string): Promise<string> {
    await page.goto("/#/boxes");
    await page.getByRole("button", { name: "+ New Box" }).click();
    await page.getByPlaceholder("e.g. Kitchen supplies").fill(label);
    await page.getByRole("button", { name: "Create", exact: true }).click();
    const row = page.locator(".catalog-row.entity-box", { hasText: label });
    await expect(row).toBeVisible();
    await row.click();
    await expect(page).toHaveURL(/#\/boxes\/BOX-/);
    return decodeURIComponent(page.url().split("#/boxes/")[1]);
}

/**
 * Add a new item to the currently-open box detail page via the inline
 * "+ Add New Item" form.
 */
export async function addItemToOpenBox(page: Page, name: string): Promise<void> {
    await page.getByText("+ Add New Item", { exact: true }).click();
    await page.getByPlaceholder("Item name").fill(name);
    await page.getByRole("button", { name: "Add Item", exact: true }).click();
    await expect(
        page.locator(".catalog-row", { hasText: name })
    ).toBeVisible();
}

/**
 * Open an item's detail page by clicking its name in a catalog row (the row's
 * click handler navigates). Waits until the detail route is active.
 */
export async function openItemDetail(page: Page, name: string): Promise<void> {
    const row = page.locator(".catalog-row", { hasText: name });
    await row.getByText(name).first().click();
    await expect(page).toHaveURL(/#\/items\//);
}
