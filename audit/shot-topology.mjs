// Sets up a throwaway operator (first-run), navigates to /infra, screenshots the topology graph.
import { chromium } from "playwright";
import fs from "node:fs";

const BASE = process.env.WEB_URL ?? "http://localhost:5087";
fs.mkdirSync("results", { recursive: true });

const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport: { width: 1440, height: 1200 } });
const page = await ctx.newPage();
await page.goto(`${BASE}/`, { waitUntil: "domcontentloaded" });
await page.waitForTimeout(3000);

const formText = (await page.locator("form").innerText().catch(() => "")).toUpperCase();
const inputs = page.locator("input");
await inputs.nth(0).fill("demo");
await inputs.nth(1).fill("demopass123");
if (formText.includes("CREATE YOUR ADMIN")) await inputs.nth(2).fill("demopass123");
await page.click('button[type="submit"]');
await page.waitForTimeout(2500);

await page.click('a[href="/infra"]').catch(() => {});
await page.waitForTimeout(3000);
console.log("url:", page.url());
await page.screenshot({ path: "results/infra-topology.png", fullPage: true });
const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
console.log("overflow:", overflow);
await browser.close();
