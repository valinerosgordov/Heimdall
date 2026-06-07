// Verifies the static-exported SPA works when served by the API (the desktop flow):
// load root -> client guard redirects to login -> log in -> client-side nav (clicks) to host + alerts.
import { chromium } from "playwright";
import fs from "node:fs";

const BASE = process.env.WEB_URL ?? "http://localhost:5087";
fs.mkdirSync("results", { recursive: true });

const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await ctx.newPage();
const errors = [];
page.on("console", (m) => { if (m.type() === "error") errors.push(m.text()); });
page.on("pageerror", (e) => errors.push("pageerror: " + e.message));

await page.goto(`${BASE}/`, { waitUntil: "domcontentloaded" });
await page.waitForTimeout(2500);
await page.locator("input").first().waitFor({ timeout: 15000 });
console.log("after root load, url =", page.url());           // expect .../login

const inputs = page.locator("input");
await inputs.nth(0).fill("admin");
await inputs.nth(1).fill("heimdall");
await page.click('button[type="submit"]');
await page.waitForFunction(() => !!localStorage.getItem("heimdall-token"), null, { timeout: 10000 });
await page.waitForTimeout(2500);
console.log("after login, url =", page.url());                // expect root /
await page.screenshot({ path: "results/spa-overview.png", fullPage: true });

const card = page.locator('a[href^="/host?name="]').first();
if (await card.count()) {
  await card.click();
  await page.waitForTimeout(2500);
  console.log("after host click, url =", page.url());
  await page.screenshot({ path: "results/spa-host.png", fullPage: true });
} else {
  console.log("no host card found");
}

await page.click('a[href="/alerts"]');
await page.waitForTimeout(2000);
console.log("after alerts nav, url =", page.url());
await page.screenshot({ path: "results/spa-alerts.png", fullPage: true });

const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
console.log("alerts overflow:", overflow, "| console errors:", errors.length, errors.slice(0, 4));
await browser.close();
