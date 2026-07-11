import { Page, TestInfo } from "@playwright/test";

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
