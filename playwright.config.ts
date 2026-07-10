import { defineConfig, devices } from "@playwright/test";

// E2E stack: Playwright (Chromium) → `vite preview` serving the built client
// (src/Client/dist) → BoxTracker.Server on :5000 → a throwaway SQLite DB.
// `vite preview` inherits `server.proxy` from src/Client/vite.config.ts, so
// `/api/*` is proxied to the server with no extra config.
export default defineConfig({
    testDir: "tests/E2E",
    fullyParallel: false,
    workers: 1, // single shared SQLite DB — see the isolation notes in the plan
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 1 : 0,
    reporter: process.env.CI
        ? [["list"], ["html", { open: "never" }]]
        : [["list"]],
    use: {
        baseURL: "http://localhost:4173",
        trace: "on-first-retry",
        screenshot: "only-on-failure",
    },
    projects: [
        { name: "desktop", use: { ...devices["Desktop Chrome"] } },
        { name: "mobile", use: { ...devices["Pixel 7"] } }, // hamburger nav, touch
    ],
    webServer: [
        {
            // In CI the server is compiled in a dedicated step beforehand, so
            // `--no-build` lets it boot in seconds instead of doing a cold
            // restore+build inside the readiness window. Locally, plain
            // `dotnet run` builds on first use.
            command: process.env.CI
                ? "dotnet run --project src/Server/BoxTracker.Server.fsproj --no-build"
                : "dotnet run --project src/Server/BoxTracker.Server.fsproj",
            url: "http://localhost:5000/api/locations",
            env: {
                // Host.CreateDefaultBuilder reads ASPNETCORE_URLS for the bind
                // address — more robust than passing --urls through `dotnet run`.
                ASPNETCORE_URLS: "http://localhost:5000",
                BOXTRACKER_DATA: process.env.E2E_DATA_DIR ?? "./tests/E2E/.data",
            },
            reuseExistingServer: !process.env.CI,
            stdout: "pipe",
            stderr: "pipe",
            timeout: 120_000,
        },
        {
            command: "npx vite preview --port 4173 --strictPort",
            cwd: "src/Client",
            url: "http://localhost:4173",
            reuseExistingServer: !process.env.CI,
            timeout: 60_000,
        },
    ],
});
