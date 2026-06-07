// Logs into the API-served SPA and screenshots the Infrastructure inventory page.
import { chromium } from "playwright";
import fs from "node:fs";

const BASE = process.env.WEB_URL ?? "http://localhost:5087";
fs.mkdirSync("results", { recursive: true });

const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
const page = await ctx.newPage();
const errors = [];
page.on("console", (m) => { if (m.type() === "error") errors.push(m.text()); });
page.on("pageerror", (e) => errors.push("pageerror: " + e.message));

await page.goto(`${BASE}/`, { waitUntil: "domcontentloaded" });
await page.waitForTimeout(2000);
await page.locator("input").first().waitFor({ timeout: 15000 });
const inputs = page.locator("input");
await inputs.nth(0).fill("admin");
await inputs.nth(1).fill("heimdall");
await page.click('button[type="submit"]');
await page.waitForFunction(() => !!localStorage.getItem("heimdall-token"), null, { timeout: 10000 });
await page.waitForTimeout(1500);

await page.click('a[href="/infra"]');
await page.waitForTimeout(2500);
console.log("infra url =", page.url());
await page.screenshot({ path: "results/infra.png", fullPage: true });

const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
console.log("infra overflow:", overflow, "| console errors:", errors.length, errors.slice(0, 4));
await browser.close();
