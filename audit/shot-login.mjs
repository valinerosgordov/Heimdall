// Mimics the desktop: load root with no token -> the client guard redirects to /login,
// which (no operator yet) shows the first-run "create admin" form. Screenshot it.
import { chromium } from "playwright";
import fs from "node:fs";

const BASE = process.env.WEB_URL ?? "http://localhost:5087";
fs.mkdirSync("results", { recursive: true });

const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();
await page.goto(`${BASE}/`, { waitUntil: "domcontentloaded" });
await page.waitForTimeout(3500);
console.log("url:", page.url());
const text = await page.locator("form").innerText().catch(() => "(no form)");
console.log("form text:", text.replace(/\n+/g, " | "));
await page.screenshot({ path: "results/login-setup.png", fullPage: true });
await browser.close();
