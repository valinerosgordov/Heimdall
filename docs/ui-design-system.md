# Heimdall — UI Design System (ROG Dark HUD)

> Design contract for the Heimdall monitoring dashboard (`Heimdall.Web`, Next.js 16 / React 19 / Tailwind v4).
> Aesthetic: **ASUS ROG** — aggressive, techy, HUD/cyber — strictly **dark**.
> Source: multi-agent research (ROG brand identity · Armoury Crate / GPU Tweak III UI · dark observability best practices · Next/Tailwind implementation).

---

## 1. Design principles

1. **Mostly-black, one screaming accent.** Chassis is near-black (`#0A0A0F`, never pure `#000`); a single saturated accent carries brand + interactivity. No rainbow-RGB — neon lives in *data lines, status pips, active states* only, never resting chrome.
2. **Calm at rest, loud only when wrong.** The dashboard is watched for 8h. Glow / pulse / max saturation are reserved for active/critical state. Idle panels are quiet hairlines. Constant glow = constant fatigue.
3. **Elevation via lightness, not shadow.** Depth = stepping ~3–6% lighter per z-level (canvas → panel → card → popover). Drop shadows barely read on near-black; avoid for layout.
4. **Translucent hairline borders.** Separators are `rgba(255,255,255,0.06–0.12)` so they read over any elevation. Active/hovered border brightens to accent + faint glow — that's how focus is signalled.
5. **Angular, not rounded.** ROG is stamped-metal and slashed. Panels use shallow `clip-path` corner cuts (8–16px), crisp 1px borders; radii stay 0–4px. Cosmetic cuts live on chrome only — **never clip data or numbers**.
6. **Color is never the sole signal.** Red+green is the #1 colorblind confusion pair (~8% of men). Danger is magenta-leaning, success teal-shifted, every status pairs color + icon + text label. Accent-red is distinguished from critical-red by *pulse/glow*, not hue.
7. **Tabular numerics everywhere.** Live metrics use mono + `tnum` so streaming digits never jitter. Off-white text (`#E8EAF0`), never `#FFF` (halation/bloom on dark).
8. **Token-driven recoloring.** The whole HUD recolors from a handful of CSS vars (`--accent`, `--accent-dim`, `--accent-glow` + shared dark base) — GPU-Tweak-III's Main/Secondary/Text model. Three palettes share one base, swap only the accent family.
9. **Performance-aware aggression.** On the live grid (dozens of animated cards) glow uses `filter: drop-shadow()` (GPU-composited), not `box-shadow` (paint thrash). uPlot/Canvas for live series, never high-density SVG. All motion gated behind `prefers-reduced-motion`.

---

## 2. Color palettes

All three share an **identical dark base + semantic status set**; only the accent family swaps. **Crimson HUD is the ship default.**

### Shared base (every palette)

| Token | Hex | Use |
|---|---|---|
| `--bg-base` | `#0A0A0F` | canvas, lowest layer (never `#000`) |
| `--bg-panel` | `#111218` | cards/panels, +1 elevation |
| `--bg-elevated` | `#16181F` | popovers/menus/hover, top layer |
| `--border-subtle` | `rgba(255,255,255,0.07)` | resting panel hairline |
| `--border-strong` | `rgba(255,255,255,0.16)` | focus ring / active border |
| `--grid-line` | `rgba(255,255,255,0.06)` | chart gridlines, bg grid |
| `--text-primary` | `#E8EAF0` | off-white body (~16:1 AAA) |
| `--text-secondary` | `#9CA3B4` | labels, units (~7:1 AAA) |
| `--text-muted` | `#5B6172` | disabled, ticks, micro chrome (decorative only) |

### Semantic status (color-blind aware, held distinct from accents)

| Token | Hex | Icon |
|---|---|---|
| `--success` | `#34D399` (teal-green) | ✓ check |
| `--warning` | `#FFB000` (amber) | ▲ triangle |
| `--danger` | `#FF2D78` (magenta-red) | ✕ cross |
| `--info` | `#5B8DEF` (blue) | i |

### Accent variants

| Palette | `--accent` | `--accent-dim` | Notes |
|---|---|---|---|
| **Crimson HUD** *(default)* | `#FF1E2D` | `#A71C1E` | Signature ROG red (hotter take on official Brown Madder `#DE272C`). Critical uses magenta-red `#FF2D78` so chrome-red ≠ critical-red. Most on-brand / highest contrast. |
| **Cyber Cyan** | `#00F0FF` | `#00B8D4` | Cool cyberpunk HUD. Accent sits clear of the warm status spectrum → best CVD separation. `--success` shifts to emerald `#22E089`. Calmest, most data-forward. |
| **Plasma Copper** | `#FF8A1E` | `#C2410C` | Molten copper/amber (ROG bronze finishes). `--warning` → `#FFD23F`, `--info` → `#4DD0E1` so they don't merge with the accent. Retro-industrial NOC wall. |

### Chart series (≤6, float above dark)

- **Crimson:** `#FF1E2D` `#4DD0E1` `#FFB74D` `#A78BFA` `#34D399` `#F06292`
- **Cyan:** `#00F0FF` `#FFB74D` `#A78BFA` `#34D399` `#F06292` `#5B8DEF`
- **Copper:** `#FF8A1E` `#4DD0E1` `#A78BFA` `#34D399` `#F06292` `#5B8DEF`

---

## 3. Typography

| Font | Role |
|---|---|
| **Orbitron** (variable) | wordmark/logo + the **single** largest hero gauge numeral only (illegible <16px) |
| **Chakra Petch** (500/600/700) | display/headings/nav — square cyber sans |
| **Rajdhani** (500/600) | dense UI labels, host metadata, status text (condensed) |
| **Inter / Geist Sans** | fallback for any long-form/paragraph text |
| **JetBrains Mono** | **all numeric data** — metrics, latency, timestamps, IPs, axis ticks. Always `font-feature-settings: 'tnum' 1, 'zero' 1` |

Body weight runs one notch heavier than light-mode (500) so thin glyphs survive on dark.

### Scale

| Token | Size | Weight | Usage |
|---|---|---|---|
| `--text-hero` | 40px / 2.5rem (lh 1.0) | 700 | Orbitron — one big host-name banner or dominant gauge numeral. One per screen. |
| `--text-display` | 28px / 1.75rem (lh 1.1) | 700 | Chakra Petch — screen title in header (OVERVIEW, HOST DETAIL). |
| `--text-metric-lg` | 22px / 1.375rem (lh 1.15) | 600 | JetBrains Mono tnum — MetricStat big number, gauge center value. |
| `--text-h2` | 18px / 1.125rem (lh 1.2) | 600 | Chakra Petch — panel/card titles. Uppercase, ls 0.06em. |
| `--text-metric` | 16px / 1rem (lh 1.2) | 500 | JetBrains Mono tnum — inline metric readouts. |
| `--text-body` | 14px / 0.875rem (lh 1.45) | 500 | Rajdhani / Inter — body, table cells, tooltips. |
| `--text-label` | 12px / 0.75rem (lh 1.3) | 600 | Rajdhani — axis labels, units, pill text. Uppercase, ls 0.10em. |
| `--text-micro` | 10px / 0.625rem (lh 1.2) | 600 | Rajdhani — HUD corner micro-labels, host-id chrome, ticks. Uppercase, ls 0.14em, muted. |

---

## 4. Geometry & effects

**Radius** — sharp by default: `--radius-sm: 2px` (chips/pills), `--radius-md: 4px` (buttons/inputs/un-cut corners). Nothing >4px. Gauges + status LEDs are the only fully-round elements.

**Corner cuts** — one var `--cut` (default 14px; `--cut-sm` 8px chips, `--cut-lg` 20px hero). Standard HUD panel clips top-left + bottom-right:
```css
clip-path: polygon(var(--cut) 0, 100% 0, 100% calc(100% - var(--cut)),
  calc(100% - var(--cut)) 100%, 0 100%, 0 var(--cut));
```
Progressive enhancement: `@supports (corner-shape: bevel)` → native `corner-shape: bevel` + `border-radius`. **Critical:** `clip-path` also clips `box-shadow` and focus outlines → apply glow via `filter: drop-shadow()` on a wrapper, and add a separate non-clipped focus-ring layer.

**Borders** — 1px hairline `--border-subtle` resting. Active/hover/selected → accent border via a 1px-inset pseudo-element rim (clip-path eats normal borders) + `accent-glow` drop-shadow. HUD corner brackets (`[ ]` reticle marks) via `::before/::after` on focused gauges/cards.

**Background** — two-layer pure-CSS, kept OUT of chart plot areas: (1) 40px grid at ~4% opacity; (2) ambient accent radial vignette at top. Optional hex-mesh ≤4% behind empty states. 8px spacing grid throughout.

**Glow** — layered, never a single shadow:
- Text: `text-shadow: 0 0 4px var(--accent), 0 0 12px var(--accent), 0 0 24px var(--accent-glow)`
- Panel: inset highlight `rgba(255,255,255,0.35)` + tight 8px halo (accent 70%) + wide 24px bloom (accent 35%), intensity driven by `--glow`.
- Live grid → `filter: drop-shadow()` only; `will-change: filter`; gate behind `prefers-reduced-motion`.
- Reserved for STATE: active/critical/down glow + pulse; focused panel rim glow; idle/healthy never glow.
- uPlot canvas: draw hook sets `ctx.shadowColor/shadowBlur` (6–10px) before stroking series; area fill = canvas linearGradient ~12% alpha → transparent, top series only.
- Gauge arc: bright glowing leading cap at the current value.

**Scanline/CRT** — fixed `repeating-linear-gradient` ~4% opacity, `pointer-events:none` — login/splash/empty only, never over live data.

---

## 5. Components

| Component | Spec (condensed) |
|---|---|
| **AppShell + TopNav** | Full-height; bgBase canvas w/ grid+vignette. 56px top bar at bgPanel + bottom hairline + accent underglow. Left: inspired-not-copied brand glyph (do **not** reproduce the ROG eye) + Orbitron wordmark. Nav: Chakra Petch uppercase OVERVIEW/HOSTS/HEALTH/ALERTS; active = brighter + 2px accent edge bar + faint glow. Right: status summary chips (n UP/WARN/DOWN), mono live-clock, palette/density toggle, pulsing **LIVE** pip. |
| **HostCard** (overview tile) | Core grid primitive. Notched panel at bgPanel in `grid auto-fill minmax(260px,1fr)`. Left 3px accent slash-bar = *worst* status on host. Header: StatusPill + host name (Rajdhani 600) + IP (mono micro). Body: full-width uPlot mini-sparkline (2px stroke, gradient area fill in status color, glowing leading dot). Footer: 3 MetricStats (CPU/RAM/NET, mono tnum). DOWN/critical → danger glow + pulse. Focusable link → Host Detail w/ non-clipped focus ring. |
| **RadialGauge** | 270° open arc (sharp-capped), not full circle. Track = faint 6% white arc; fill hue shifts by **threshold** (success ≤70%, warning 70–90%, danger 90–100%) w/ glowing leading cap. Center = value (mono tnum, `--text-metric-lg`/`--text-hero`) + unit (Rajdhani micro). visx Arc or cheap conic-gradient ring. Value eases 200–400ms. Host Detail = a top row of these (GPU-Tweak-III style). |
| **TimeSeriesChart** | uPlot Canvas. Notched panel; Chakra Petch uppercase title + 1px accent rule. Dark plot bg, gridlines 6% white, axis `#5A5A5A`, ticks mono `#C0C0C0`. 2–3px neon series from chartSeries (metric under alert = danger) w/ subtle shadowBlur; top series gets 12%-alpha gradient fill. Glowing latest-point dot. Tooltip on bgElevated + border-strong + mono values. Stacked charts share cursor via `uPlot.sync`. Legend = status chips + tnum current values. |
| **StatusPill** | Universal CVD-safe atom. **Never color-only:** LED dot + glyph (✓ UP / ▲ WARN / ✕ DOWN / i info) + uppercase Rajdhani label. Semantic colors (not the accent → status never reads as clickable). Dot glows+pulses only for active critical/down. Sizes: sm (12px dot, hover label) dense tables; md (dot+glyph+label) cards/headers. |
| **AlertRow** | Severity-led. 4px angular left bar by severity. Badge + title (Rajdhani) + source host link + mono timestamp + duration. Active critical → danger glow + pulse; warning → faint steady glow; resolved/history → muted/desaturated, no glow. Active vs History split by slashed divider. Hover lifts to bgElevated. ~44px rows, zebra via 2–3% white, 12% hairlines. |
| **MetricStat** | Standalone KPI. Mono tnum value (`--text-metric-lg`/`--text-hero`), off-white. Above: uppercase Rajdhani micro label (secondary). Inline: unit (mono muted) + optional delta caret + % (success/danger, caret = non-color signal). Optional 2px trailing sparkline. Tabular digits → no width shift on update. |
| **HealthCheckRow / Board** | Dense table-grid. Cols: name + StatusPill, endpoint/IP (mono micro), latency (mono tnum, threshold-colored + inline bar), uptime% (mono), uptime sparkline. Shallow-notched chips at bgPanel; DOWN glow+pulse danger, degraded steady warning, UP flat. Zebra 2–3%, 12% hairlines, sticky Chakra Petch header + sort/filter strip. Reflows to stacked cards <720px. |
| **HudPanel** (base primitive) | The single `@layer components` class everything composes from: bg-panel fill, clip-path notch (`--cut`), 1px-inset accent rim, corner-bracket marks, `--glow` opacity var, `filter: drop-shadow()` glow, non-clipped focus ring, optional uppercase header + accent rule. HostCard / charts / gauges / alert cards are all variants. |

---

## 6. Motion

- **Live value interpolation:** gauges ease arc, sparklines tween, MetricStat numbers count toward new value over 200–400ms — never hard jumps. No continuous looping motion in the periphery (NOC wall).
- **Status pulse:** `pulse-glow` 1.5s ease-in-out infinite, ONLY on active critical/down + firing alerts — the single "act now" cue.
- **Hover:** panel lifts one elevation step + border → border-strong + accent top-edge bar reveals, ~120ms ease-out, transform/filter/opacity only, zero layout shift. Primary buttons run accent→accent-dim gradient sweep.
- **First paint:** line graphs draw stroke left-to-right; sparkline tiles fade/slide in on reflow — one-shot, not looping.
- **Focus:** distinct non-clipped `--border-strong` outer ring + accent rim glow (clip-path eats normal outlines) — always visible.
- **`prefers-reduced-motion: reduce`** KILLS all pulse/glow-animation/count-up/draw-in/scanline; values snap, glow static, indicators rely on color+icon+label. Never animate large-panel backgrounds (repaint jank).

---

## 7. Implementation

### `globals.css` — tokens + base + HudPanel

```css
:root {
  /* ---- Shared dark base (identical across all 3 palettes) ---- */
  --bg-base: #0A0A0F;          /* canvas, lowest z-layer, never pure #000 */
  --bg-panel: #111218;         /* cards/panels, +1 elevation */
  --bg-elevated: #16181F;      /* popovers/menus/hover, top z-layer */
  --border-subtle: rgba(255,255,255,0.07);
  --border-strong: rgba(255,255,255,0.16);
  --grid-line: rgba(255,255,255,0.06);

  --text-primary: #E8EAF0;     /* off-white, NOT #FFF (halation) */
  --text-secondary: #9CA3B4;
  --text-muted: #5B6172;

  /* ---- Semantic status (color-blind aware, distinct from accents) ---- */
  --success: #34D399;          /* + check icon */
  --warning: #FFB000;          /* + triangle icon */
  --danger:  #FF2D78;          /* MAGENTA-leaning red + X icon (CVD-safe) */
  --info:    #5B8DEF;          /* + i icon */

  /* ---- ACCENT: Crimson HUD (DEFAULT) ---- */
  --accent: #FF1E2D;
  --accent-dim: #A71C1E;
  --accent-glow: rgba(255,30,45,0.55);

  /* ---- Chart series ---- */
  --series-1: #FF1E2D; --series-2: #4DD0E1; --series-3: #FFB74D;
  --series-4: #A78BFA; --series-5: #34D399; --series-6: #F06292;

  /* ---- Geometry / motion knobs ---- */
  --cut: 14px; --cut-sm: 8px; --cut-lg: 20px;
  --radius-sm: 2px; --radius-md: 4px;
  --glow: 0;                   /* 0 idle -> 1 active/critical */
}

[data-palette="cyan"] {
  --accent: #00F0FF; --accent-dim: #00B8D4; --accent-glow: rgba(0,240,255,0.50);
  --success: #22E089;
  --series-1:#00F0FF; --series-2:#FFB74D; --series-3:#A78BFA;
  --series-4:#34D399; --series-5:#F06292; --series-6:#5B8DEF;
}

[data-palette="copper"] {
  --accent: #FF8A1E; --accent-dim: #C2410C; --accent-glow: rgba(255,138,30,0.50);
  --warning: #FFD23F; --info: #4DD0E1;
  --series-1:#FF8A1E; --series-2:#4DD0E1; --series-3:#A78BFA;
  --series-4:#34D399; --series-5:#F06292; --series-6:#5B8DEF;
}

body {
  background-color: var(--bg-base);
  background-image:
    radial-gradient(ellipse at top, color-mix(in srgb, var(--accent) 8%, transparent), transparent 60%),
    linear-gradient(var(--grid-line) 1px, transparent 1px),
    linear-gradient(90deg, var(--grid-line) 1px, transparent 1px);
  background-size: auto, 40px 40px, 40px 40px;
  color: var(--text-primary);
}

.hud-panel {
  position: relative;
  background: var(--bg-panel);
  border: 1px solid var(--border-subtle);
  clip-path: polygon(var(--cut) 0, 100% 0, 100% calc(100% - var(--cut)),
    calc(100% - var(--cut)) 100%, 0 100%, 0 var(--cut));
  filter: drop-shadow(0 0 calc(12px * var(--glow)) var(--accent-glow));
  transition: filter .12s ease-out, background .12s ease-out;
}
.hud-panel:hover, .hud-panel[data-active="true"] { --glow: .6; background: var(--bg-elevated); }
.hud-panel[data-status="down"] { --glow: 1; filter: drop-shadow(0 0 14px color-mix(in srgb, var(--danger) 55%, transparent)); }

@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after { animation: none !important; transition: none !important; }
}
```

### Tailwind v4 (`@theme inline`, CSS-first — no `tailwind.config.js` for tokens)

```css
@import "tailwindcss";

@theme inline {
  /* Fonts from next/font/google variables on <html> */
  --font-display: var(--font-orbitron);   /* logo / hero numeral only */
  --font-heading: var(--font-chakra);     /* titles, nav */
  --font-ui:      var(--font-rajdhani);   /* dense labels, body */
  --font-mono:    var(--font-jetbrains);  /* all numeric data */

  --color-bg:        #0A0A0F;
  --color-panel:     #111218;
  --color-elevated:  #16181F;
  --color-border:    rgba(255,255,255,0.07);
  --color-border-strong: rgba(255,255,255,0.16);
  --color-grid:      rgba(255,255,255,0.06);

  --color-text:      #E8EAF0;
  --color-muted:     #9CA3B4;
  --color-faint:     #5B6172;

  --color-success:   #34D399;
  --color-warning:   #FFB000;
  --color-danger:    #FF2D78;
  --color-info:      #5B8DEF;

  /* Accent maps to the live CSS var so [data-palette] swaps recolor utilities */
  --color-accent:     var(--accent);
  --color-accent-dim: var(--accent-dim);

  --color-series-1: var(--series-1); --color-series-2: var(--series-2);
  --color-series-3: var(--series-3); --color-series-4: var(--series-4);
  --color-series-5: var(--series-5); --color-series-6: var(--series-6);

  --radius-sm: 2px;
  --radius-md: 4px;

  --animate-pulse-glow: pulse-glow 1.5s ease-in-out infinite;
}

@keyframes pulse-glow { 0%,100% { opacity:.55 } 50% { opacity:1 } }

/* DARK MODE ONLY — no dark: variant needed. */
```

### Charts

uPlot (Canvas) for all live/streaming time-series and sparklines — series colors pulled from `--series-*` via `getComputedStyle`, glow via canvas `shadowBlur`. visx `Arc` (or conic-gradient) for radial gauges. Avoid high-density SVG for streaming data.

---

## 8. Mockups

### Overview grid
```
+==========================================================================+
| [//] HEIMDALL    OVERVIEW  HOSTS  HEALTH  ALERTS     o12 UP  !2 WARN x1 DOWN |
|                                                  o LIVE   14:22:07  [palette]|
+==========================================================================+
|  /---------------\   /---------------\   /---------------\   /-------------\ |
|  |o UP  web-01   |   |o UP  web-02   |   |! WARN db-01  |   |x DOWN cache-1|*|
|  |     10.0.0.11 |   |     10.0.0.12 |   |    10.0.0.21 |   |   10.0.0.31  |*|
|  | /\    /\_/\   |   |  _/\_/\__/\   |   | /\/\  /\/\/\ |   |  ___...____  |*|
|  |/  \/\/     \. |   | /         \.  |   |/        \/\. |   | (no signal)  |*|
|  | CPU RAM  NET  |   | CPU RAM  NET  |   | CPU RAM NET  |   | CPU RAM  NET |*|
|  | 34% 61% 88M/s |   | 28% 55% 40M/s |   | 91% 80% 12M/s|   |  --  --  --  |*|
|  \---------------/   \---------------/   \---------------/   \-------------/ |
|   ^accent rim/hover    ^teal status        ^amber slash-bar  ^danger glow+pulse|
+==========================================================================+
 notched panels (clip-path) | left slash-bar=worst status | sparkline=uPlot Canvas
```

### Host detail
```
+==========================================================================+
| [//] HEIMDALL   < HOSTS / web-01            o UP   10.0.0.11   uptime 41d  |
+==========================================================================+
|  GAUGES (GPU-Tweak-III top row, 270deg open arcs, glow leading cap)        |
|   .-''-.        .-''-.        .-''-.        .-''-.        .-''-.            |
|  /  34  \      /  61  \      /  57  \      /  72  \      /  88  \           |
| |  %CPU  |    |  %RAM  |    | %DISK  |    | C TEMP |    | MB/s  |           |
|  \ teal /      \ teal /      \ teal /      \ amber/      \ teal /  <-thresh  |
|   '-..-'        '-..-'        '-..-'        '-..-'        '-..-'            |
+--------------------------------------+-----------------------------------+|
| CPU  [ ===== uPlot line ===== ]  72% | NETWORK [ ===== uPlot ===== ] 88M/s||
| 100|          /\        /\.         | 200|        /\      .            ||
|  50|   /\__/\/  \__/\__/   \___      |  100| __/\__/  \__/\___/\__       ||
|   0|_______________________________ |   0|______________________________||
|     14:00   14:10   14:20  (mono)    |     14:00  14:10  14:20           ||
+--------------------------------------+-----------------------------------+|
| RAM  [ ===== uPlot area-fill ===== ] | DISK I/O [ ===== uPlot ===== ]     ||
|  ...synced crosshair across all 4 (uPlot.sync), faint 6% gridlines...     ||
+==========================================================================+
 quiet chrome (gray axes/ticks) | neon lives in series lines + gauge arcs
```

### Alerts board
```
+==========================================================================+
| [//] HEIMDALL   ALERTS            [ ACTIVE (3) ] \ history    filter: all v|
+==========================================================================+
| SEV     ALERT                         HOST       STARTED      DURATION     |
|--------------------------------------------------------------------------|
||x|[CRIT] cache-1 unreachable           cache-1   14:21:55     00:00:12 *** | <- magenta bar, glow+PULSE
||!|[WARN] db-01 CPU > 90% (91%)         db-01     14:19:03     00:03:04  *  | <- amber bar, faint glow
||!|[WARN] web-03 latency > 500ms        web-03    14:15:40     00:06:27  *  |
|--------------------------------------------------------------------------|
|  ~~~~~~~~~~~~~~~~~~~~~~ \\\ HISTORY \\\ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ |
||.|[crit] app-02 OOM kill               app-02    13:02:11   resolved 14:01 | <- muted, no glow
||.|[warn] disk-02 disk > 80%            disk-02   11:48:30   resolved 12:10 |
||.|[info] deploy web-* v2.3.1           cluster   09:30:00   ack            |
+==========================================================================+
 left severity bar | StatusPill = dot+glyph+LABEL (never color-only) | mono times
```

---

## 9. Accessibility (non-negotiable)

- **Never `#000` bg, never `#FFF` body text.** Canvas `#0A0A0F`; text `#E8EAF0` (anti-halation). `#FFF` only on the single largest hero numeral/title per screen.
- **Contrast:** text/bg ≥4.5:1; large/heading ≥3:1; the metric users stare at for hours ≥7:1 (AAA). On `#0A0A0F`: `#E8EAF0` ~16:1, `#9CA3B4` ~7:1, `#5B6172` ~3.4:1 (decorative/disabled only). Translucent hairlines below text minimums → separators only.
- **Status never color-only:** every StatusPill = dot + glyph + uppercase label; critical further distinguished by pulse+glow, not hue.
- **CVD-safe semantics:** danger magenta-red `#FF2D78`, success teal `#34D399`/`#22E089`, warning amber `#FFB000`, info blue `#5B8DEF` — triad survives protanopia/deuteranopia/tritanopia. Accent held distinct from status.
- **Charts:** series desaturated/lightened to float above dark, 2–3px (1px vanishes); cap categorical series at 6; chrome quiet (gridlines 6%, axis `#5A5A5A`, ticks `#C0C0C0`). Single-metric gauges = single-hue sequential + threshold hue-shift, not rainbow.
- **Motion/glow restraint:** accents desaturated ~20–30% vs light-mode equivalents; glow confined to active/critical. `prefers-reduced-motion` disables all motion; users rely on color+icon+label.
- **Focus with clip-path:** notched panels add a SEPARATE non-clipped focus ring (`--border-strong` outer outline). Decorative cuts never clip data/numbers/interactive targets.
- **Live numerics:** tabular figures (`'tnum' 1, 'zero' 1`) so streaming values don't reflow. Orbitron restricted to logo + largest numerals (illegible <16px); dense labels use Rajdhani/Inter ≥500.
