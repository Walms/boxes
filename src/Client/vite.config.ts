import { defineConfig } from "vite";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
    clearScreen: false,
    plugins: [tailwindcss()],
    server: {
        proxy: {
            "/api": "http://localhost:5000",
        },
        watch: {
            ignored: ["**/*.fs"],
        },
    },
});
