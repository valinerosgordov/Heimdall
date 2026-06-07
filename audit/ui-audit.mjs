import { chromium } from "playwright";
import fs from "node:fs";

const BASE = process.env.WEB_URL ?? "http://localhost:3000";
const OUT = "results";
fs.mkdirSync(OUT, { recursive: true });

const VIEWPORTS = [
  { name: "desktop", width: 1440, height: 900 },
  { name: "mobile", width: 390, height: 844 },
];

const ROUTES = [
  { name: "login", path: "/login" },
  { name: "overview", path: "/" },
  { name: "host-detail", path: "/host?name=VALINERPC" },
  { name: "alerts", path: "/alerts" },
];

async function measure(page) {
  return page.evaluate(() => {
    const de = document.documentElement;
    const overflow = de.scrollWidth - de.clientWidth;
    const brokenImages = [...document.images].filter((i) => i.src && i.naturalWidth === 0).map((i) => i.src);
    const smallTargets = [];
    for (const el of document.querySelectorAll("a,button,input,select,[role=button]")) {
      const r = el.getBoundingClientRect();
      if (r.width > 0 && r.height > 0 && (r.width < 44 || r.height < 44)) {
        smallTargets.push({ tag: el.tagName.toLowerCase(), text: (el.textContent || "").trim().slice(0, 20), w: Math.round(r.width), h: Math.round(r.height) });
      }
    }
    return { overflow, brokenImages, smallTargets };
  });
}

const results = [];
const browser = await chromium.launch();

for (const vp of VIEWPORTS) {
  const context = await browser.newContext({ viewport: { width: vp.width, height: vp.height } });
  const page = await context.newPage();

  // Log in (fresh localStorage per context).
  await page.goto(`${BASE}/login`, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(800);
  const inputs = page.locator("input");
  await inputs.nth(0).fill("admin");
  await inputs.nth(1).fill("heimdall");
  await page.click('button[type="submit"]');
  // Fail loudly if login didn't persist a token (don't silently screenshot the login page).
  await page.waitForFunction(() => !!localStorage.getItem("heimdall-token"), null, { timeout: 10000 });
  await page.waitForTimeout(500);

  for (const route of ROUTES) {
    const consoleErrors = [];
    const onConsole = (m) => { if (m.type() === "error") consoleErrors.push(m.text()); };
    const onPageError = (e) => consoleErrors.push("pageerror: " + e.message);
    page.on("console", onConsole);
    page.on("pageerror", onPageError);

    try {
      await page.goto(`${BASE}${route.path}`, { waitUntil: "domcontentloaded" });
      await page.waitForTimeout(3000); // let SSE/poll render real data
      const m = await measure(page);
      const screenshot = `${OUT}/${route.name}-${vp.name}.png`;
      await page.screenshot({ path: screenshot, fullPage: true });
      results.push({ route: route.name, viewport: vp.name, ...m, consoleErrors });
      console.log(`${route.name} @ ${vp.name}: overflow=${m.overflow}px console=${consoleErrors.length} broken=${m.brokenImages.length} small=${m.smallTargets.length}`);
    } catch (err) {
      results.push({ route: route.name, viewport: vp.name, error: String(err) });
      console.log(`${route.name} @ ${vp.name}: ERROR ${err}`);
    } finally {
      page.off("console", onConsole);
      page.off("pageerror", onPageError);
    }
  }
  await context.close();
}

await browser.close();
fs.writeFileSync(`${OUT}/results.json`, JSON.stringify(results, null, 2));
console.log("WROTE results/results.json");
