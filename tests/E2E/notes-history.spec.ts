import { test, expect } from "@playwright/test";
import {
    alnumToken,
    createLocation,
    createBox,
    addItemToOpenBox,
    openItemDetail,
} from "./helpers";

// The notes section (shared by box/item/location detail pages) and the move
// history modal. Notes are plain CRUD against /api/notes; history renders the
// append-only move log, which is the observable heart of the event-sourced
// model. Desktop-only like the other flow specs.

test.beforeEach(({}, testInfo) => {
    test.skip(testInfo.project.name !== "desktop", "notes/history coverage runs desktop-only");
});

test("note lifecycle on a box: add, edit, delete", async ({ page }) => {
    const tok = alnumToken();
    const noteText = `Fragile contents ${tok}`;
    const editedText = `Very fragile contents ${tok}`;

    // createBox leaves us on the box detail page, which hosts a notes section.
    await createBox(page, `Box ${tok}`);
    await expect(page.getByRole("heading", { name: /^Notes \(0\)$/ })).toBeVisible();
    await expect(page.getByText("No notes yet")).toBeVisible();

    // Add.
    await page.getByRole("button", { name: "+ Add Note" }).click();
    await page.getByPlaceholder("Write a note...").fill(noteText);
    await page.getByRole("button", { name: "Save", exact: true }).click();
    await expect(page.getByText(noteText, { exact: true })).toBeVisible();
    await expect(page.getByRole("heading", { name: /^Notes \(1\)$/ })).toBeVisible();
    await expect(page.getByText(/^Added /)).toBeVisible();

    // Edit. The edit textarea is prefilled with the current content.
    await page.getByRole("button", { name: "Edit", exact: true }).click();
    const textarea = page.locator("textarea");
    await expect(textarea).toHaveValue(noteText);
    await textarea.fill(editedText);
    await page.getByRole("button", { name: "Save", exact: true }).click();
    await expect(page.getByText(editedText, { exact: true })).toBeVisible();
    await expect(page.getByText(noteText, { exact: true })).toHaveCount(0);
    await expect(page.getByText(/· Edited /)).toBeVisible();

    // Delete.
    await page.getByRole("button", { name: "Delete", exact: true }).click();
    await expect(page.getByText(editedText, { exact: true })).toHaveCount(0);
    await expect(page.getByRole("heading", { name: /^Notes \(0\)$/ })).toBeVisible();
    await expect(page.getByText("No notes yet")).toBeVisible();
});

test("notes persist across a reload on an item detail page", async ({ page }) => {
    const tok = alnumToken();
    const itemName = `Vase ${tok}`;
    const noteText = `Wrapped in bubble wrap ${tok}`;

    await createBox(page, `Box ${tok}`);
    await addItemToOpenBox(page, itemName);
    await openItemDetail(page, itemName);

    await page.getByRole("button", { name: "+ Add Note" }).click();
    await page.getByPlaceholder("Write a note...").fill(noteText);
    await page.getByRole("button", { name: "Save", exact: true }).click();
    await expect(page.getByText(noteText, { exact: true })).toBeVisible();

    // The note came back from the server, not just local state.
    await page.reload();
    await expect(page.getByText(noteText, { exact: true })).toBeVisible();
    await expect(page.getByRole("heading", { name: /^Notes \(1\)$/ })).toBeVisible();
});

test("box history shows creation and the move to a location", async ({ page }) => {
    const tok = alnumToken();
    const locCode = `H${tok}`.slice(0, 12).toUpperCase();
    const locName = `Garage ${tok}`;
    const boxLabel = `Box ${tok}`;

    await createLocation(page, locCode, locName);
    const boxId = await createBox(page, boxLabel);

    // Assign the box to the location via the detail page's assign dropdown.
    await page.getByRole("button", { name: /Unassigned/ }).click();
    await page
        .locator(".dropdown-content")
        .getByText(locName, { exact: true })
        .click();
    await expect(page.getByRole("button", { name: new RegExp(locName) })).toBeVisible();

    await page.getByRole("button", { name: /Actions/ }).click();
    await page.getByText("View History", { exact: true }).click();

    const modal = page.locator(".modal-open");
    await expect(modal.getByRole("heading", { name: "History" })).toBeVisible();
    await expect(modal.getByText(boxLabel)).toBeVisible();
    await expect(modal.getByText("Created", { exact: true })).toBeVisible();
    await expect(modal.getByText(`Moved to ${locName}`, { exact: true })).toBeVisible();

    // Close via the ✕ button.
    await modal.getByRole("button", { name: "✕" }).click();
    await expect(page.locator(".modal-open")).toHaveCount(0);
});

test("item history records each move in order", async ({ page }) => {
    const tok = alnumToken();
    const itemName = `Torch ${tok}`;
    const boxA = await createBox(page, `BoxA ${tok}`);
    const boxB = await createBox(page, `BoxB ${tok}`);

    // A bare hash change right after the app's own navigation can be missed
    // by the router; force a full load and wait for box A's detail before
    // adding, or the item lands in the still-loaded box B.
    await page.goto(`/#/boxes/${boxA}`);
    await page.reload();
    await expect(page.getByRole("heading", { name: `BoxA ${tok}` })).toBeVisible();
    await addItemToOpenBox(page, itemName);
    await openItemDetail(page, itemName);

    // Move it to box B so the log has two moves.
    await page.getByRole("button", { name: /Actions/ }).click();
    await page.getByText("Move to box", { exact: true }).click();
    const moveModal = page.locator(".modal-open");
    await moveModal.getByText(`BoxB ${tok}`, { exact: true }).click();
    await moveModal.getByRole("button", { name: "Move", exact: true }).click();
    await expect(page.locator(".modal-open")).toHaveCount(0);

    await page.getByRole("button", { name: /Actions/ }).click();
    await page.getByText("View History", { exact: true }).click();

    // Chronological, oldest first: created → moved to A → moved to B.
    const modal = page.locator(".modal-open");
    const entries = modal.locator("li p").filter({ hasText: /^(Created|Moved to )/ });
    await expect(entries).toHaveText([
        "Created",
        `Moved to ${boxA}`,
        `Moved to ${boxB}`,
    ]);
});
