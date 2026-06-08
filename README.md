# Heimdall

Self-hosted monitoring for your servers and projects — a focused mix of Prometheus (collection) and Grafana (visualization), built as one coherent stack. The watchman of your infrastructure.

> **Status:** Phases 0–11. A cross-platform `.NET 10` agent enrolls with a per-host key and pushes CPU/memory/disk/network/uptime to the API, which stores samples in TimescaleDB. A scheduler probes HTTP/TCP health-check targets, and a threshold **alert engine** fires/resolves rules with Telegram + email channels. The Next.js dashboard (ASUS ROG dark HUD) shows a **live overview grid via Server-Sent Events**, per-host detail with radial gauges and **auto-downsampled** time-series, and an alerts board. Phases 7–11 add an **Infrastructure** view — a server inventory with cost & renewal-date tracking and a visual topology graph — plus **agent auto-discovery** (hosts report their own OS/cores/RAM/disk/listening ports, so the inventory fills itself) and **first-run admin setup** (you create your account on first launch; no shipped default password, JWT secret auto-generated). Hardened with **JWT auth, per-IP rate limiting, OpenTelemetry traces/metrics, retention policies, and Problem Details** exception handling. **114 tests** (unit + architecture + Testcontainers integration) pass.

---

## Architecture

```
┌──────────────┐   push (HTTP+JSON, X-Heimdall-Key)   ┌──────────────────────────┐
│ Heimdall.Agent│ ───────────────────────────────────► │  Heimdall.Api            │
│ (per host)    │   cpu.usage / memory.usage           │  ─ ingest (COPY)         │──► TimescaleDB
│ kernel32 P/Inv│                                       │  ─ query                 │   hypertable
└──────────────┘                                       │  ─ ingest-key auth       │   metric_samples
                                                        └──────────┬───────────────┘
                                          REST (camelCase JSON)     │
                                                        ┌──────────▼───────────────┐
                                                        │ Heimdall.Web (Next.js 16) │  live uPlot charts,
                                                        └──────────────────────────┘   ROG dark HUD
```

**Solution layout** — Clean Architecture + Vertical Slices (modules as feature folders inside layers):

| Project | Role |
|---|---|
| `Heimdall.Domain` | SharedKernel (`Result<T>`, `Error`, `Entity`); Hosts, Metrics, HealthChecks, Alerting and Inventory (servers + topology links) domain — value objects, invariants |
| `Heimdall.Application` | Explicit handlers (ingest, queries, health-checks, alerts, inventory + auto-discovery, auth/first-run setup), repository abstractions. No MediatR. Depends only on Domain + Contracts. |
| `Heimdall.Infrastructure` | Npgsql data source, Dapper repositories, binary-`COPY` inserts, idempotent Timescale schema initializer |
| `Heimdall.Contracts` | Agent↔server wire DTOs + `System.Text.Json` source-gen context (camelCase, AOT-ready) |
| `Heimdall.Api` | Minimal API, `TypedResults`, Problem Details (RFC 9457), CORS, JWT auth — and serves the static-exported dashboard from `wwwroot` (single origin) |
| `Heimdall.Agent` | Worker Service; enrolls for a per-host key, then pushes CPU/memory/disk/network/uptime. Windows CPU/mem via kernel32 P/Invoke, Linux via `/proc`; disk/net/uptime cross-platform (DriveInfo / NetworkInterface / TickCount64) |
| `Heimdall.Web` | Next.js 16 / React 19 / Tailwind v4 dashboard (static-exported SPA). Live overview grid (SSE) + health board, host detail with radial gauges & ranged uPlot charts, runtime palette switch (Crimson/Cyan/Copper). ROG dark theme. |
| `Heimdall.Desktop` | WPF + WebView2 shell — boots the backend and shows the dashboard in a native window. The packaged desktop app. |

UI design contract: [docs/ui-design-system.md](docs/ui-design-system.md).

---

## Tech stack

- **Backend:** .NET 10 / C# 14, Minimal API, Result pattern, Npgsql + Dapper.
- **Storage:** TimescaleDB (PostgreSQL 17 + hypertables).
- **Agent:** .NET Worker Service, kernel32 P/Invoke (no external deps).
- **Frontend:** Next.js 16, React 19, Tailwind CSS v4, uPlot.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) (`dotnet --version` ≥ 10)
- [Docker Desktop](https://www.docker.com/) (for TimescaleDB)
- [Node.js 22+](https://nodejs.org/) (for the web dashboard)

---

## Running locally

From the repository root:

### 1. Start TimescaleDB
```bash
docker compose up -d
```

### 2. Start the API  (http://localhost:5087)
```bash
dotnet run --project src/Heimdall.Api
```
On first start it creates the schema (`hosts`, `metric_samples` hypertable) automatically.

### 3. Start the agent  (reports this machine's CPU/RAM every 5s)
```bash
dotnet run --project src/Heimdall.Agent
```

### 4. Start the web dashboard  (http://localhost:3100)
```bash
cd src/Heimdall.Web
npm install     # first time only
npm run dev
```

Open <http://localhost:3100>. On **first run** you create your admin account (username + password), then your host appears within a few seconds with live charts. The web uses a dedicated port (3100) to avoid colliding with other local dev servers.

Tear down the database with `docker compose down` (add `-v` to wipe stored metrics).

---

## Run as a desktop app (Windows)

Heimdall ships as a **native desktop app** — a WPF + WebView2 shell (`Heimdall.Desktop`) that boots the backend and shows the dashboard in its own window (no browser, no console).

Build once, then create the Desktop shortcut:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-heimdall.ps1     # publish API+Agent+Desktop, export the dashboard into the API
powershell -ExecutionPolicy Bypass -File scripts\create-shortcut.ps1    # creates the Desktop "Heimdall" icon
```

Then **double-click the “Heimdall” icon**. The app:
1. starts Docker Desktop + TimescaleDB if needed,
2. launches the published API (which also serves the dashboard) and the agent,
3. shows the dashboard in a native window — on first run, **create your admin account**.

**Closing the window stops everything** (API, agent, TimescaleDB; data is preserved). The agent runs natively, so it measures *this machine* (a container would report the container's metrics).

Prefer a browser to the app window? `scripts\start-heimdall.ps1` runs the same backend and opens <http://localhost:5087>.

### Sharing the app with others

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1   # -> release\Heimdall-<timestamp>.zip
```

This produces a **self-contained** zip (no .NET install needed on the target). The recipient unzips it and double-clicks **`Run Heimdall.bat`**. Requirements on their machine:

- **Docker Desktop** (running) — Heimdall stores metrics in TimescaleDB, which runs in Docker. This is the one hard dependency.
- **WebView2 Runtime** — preinstalled on Windows 11; on Windows 10 it's a quick install.

No credentials ship in the build: on first launch the recipient **creates their own admin account**, and the JWT signing secret is generated locally. Nothing secret is baked in.

---

## API

Dashboard read/admin endpoints (`/api/hosts`, `/api/overview`, `/api/stream/*`, `/api/healthchecks`, `/api/alerts*`, `/api/servers*`) require a JWT `Bearer` token; agent endpoints (`/api/enroll`, `/api/ingest/*`) use key auth.

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `GET` | `/health` | — | Liveness |
| `GET` | `/api/auth/status` | — | Whether an operator exists yet (drives login vs first-run setup) |
| `POST` | `/api/auth/setup` | — | First run: create the operator account; 409 once one exists (rate-limited) |
| `POST` | `/api/auth/login` | — | Exchange operator credentials for a JWT (rate-limited) |
| `POST` | `/api/enroll` | `X-Heimdall-Enrollment-Key` | Issue a per-host agent key (returned once) |
| `POST` | `/api/ingest/metrics` | `X-Heimdall-Key` (per-host) | Push a batch of samples |
| `GET` | `/api/hosts` | — | List monitored hosts |
| `GET` | `/api/hosts/{hostName}/metrics?names=…&minutes=15&maxPoints=500` | — | Query time series (auto-downsampled when the window is wide) |
| `GET` | `/api/overview` | — | Overview snapshot: hosts + latest values + online state + health |
| `GET` | `/api/stream/overview` | — | Live overview via Server-Sent Events (2s cadence) |
| `POST` | `/api/healthchecks` | — | Create a health-check target (`Http`/`Tcp`) |
| `GET` | `/api/healthchecks` | — | Status board (latest up/down + latency) |
| `DELETE` | `/api/healthchecks/{id}` | — | Remove a target |
| `GET` | `/api/healthchecks/{id}/history?minutes=60` | — | Probe result history |
| `POST` | `/api/alerts/rules` | — | Create an alert rule (`gt`/`lt`/`gte`/`lte`, `warning`/`critical`) |
| `GET` | `/api/alerts/rules` | — | List alert rules |
| `DELETE` | `/api/alerts/rules/{id}` | — | Remove a rule |
| `GET` | `/api/alerts?limit=100` | — | Alerts — firing first, then resolved |
| `GET` | `/api/servers` | JWT | Inventory: servers (specs, cost, renewal date, discovered fields) + topology links |
| `POST`·`PUT`·`DELETE` | `/api/servers[/{id}]` | JWT | Create / update / delete a server |
| `POST`·`DELETE` | `/api/servers/links[/{id}]` | JWT | Create / delete a topology link |
| `POST` | `/api/ingest/inventory` | `X-Heimdall-Key` | Agent auto-discovery: upsert a host's OS / specs / ports |

`/api/enroll` and `/api/ingest/*` are rate-limited to 120 requests / 10s per client IP.

**Smoke test** (enroll, then push):
```bash
KEY=$(curl -s -X POST http://localhost:5087/api/enroll \
  -H "Content-Type: application/json" -H "X-Heimdall-Enrollment-Key: dev-local-enrollment-key-change-me" \
  -d '{"hostName":"demo"}' | python -c "import sys,json;print(json.load(sys.stdin)['agentKey'])")

curl -X POST http://localhost:5087/api/ingest/metrics \
  -H "Content-Type: application/json" -H "X-Heimdall-Key: $KEY" \
  -d "{\"hostName\":\"demo\",\"samples\":[{\"metric\":\"cpu.usage\",\"value\":42.5,\"timestampUnixMs\":0}]}"
```

---

## Configuration

- **API** — `src/Heimdall.Api/appsettings.Development.json`: connection string, CORS origins, the dev `Heimdall:EnrollmentKey`, optional `Heimdall:Jwt:LifetimeHours`. **No operator credentials or signing secret live here** — the admin account is created on first run (stored in the DB), and the JWT secret is auto-generated under `%LOCALAPPDATA%/Heimdall/jwt.secret` (override with `Heimdall:Jwt:Secret` to pin it).
- **Agent** — `src/Heimdall.Agent/appsettings.json`: `ServerUrl`, `EnrollmentKey`, optional `HostName` (defaults to the machine name), `IntervalSeconds`. On first run the agent enrolls and caches its per-host key under `%LOCALAPPDATA%/Heimdall/<host>.key` (Linux: `~/.local/share/Heimdall/`).
- **Web** — `src/Heimdall.Web/.env.local`: `NEXT_PUBLIC_API_URL`.
- **Alert channels (optional)** — `Heimdall:Telegram:{BotToken,ChatId}` and/or `Heimdall:Email:{Host,Port,From,To,User,Password}`. Unconfigured channels are skipped silently.
- **Telemetry** — OpenTelemetry traces + metrics export to the console by default; set up an OTLP collector and swap `AddConsoleExporter()` for `AddOtlpExporter()` for Jaeger/Seq.

> `appsettings.Development.json` is **gitignored**. On a fresh clone, copy the template first:
> ```bash
> cp src/Heimdall.Api/appsettings.Development.json.example src/Heimdall.Api/appsettings.Development.json
> ```
> It carries only the local DB connection and the dev enrollment key. Your admin login is created on first run and the JWT secret is generated locally — **nothing secret is committed**. In production use User Secrets / Key Vault.

---

## Roadmap

- [x] **Phase 0** — Foundation: solution, SharedKernel, Timescale, schema initializer.
- [x] **Phase 1** — Tracer bullet: agent → ingest → store → query → live chart.
- [x] **Phase 2** — Full collection (CPU/memory/disk/network/uptime, Windows + Linux agent) + host enrollment with per-agent keys.
- [x] **Phase 3** — Health-checks: HTTP/TCP probe scheduler, targets CRUD, status board + history.
- [x] **Phase 4** — Dashboards: live overview grid (SSE) + health board, host detail (radial gauges + ranged time-series), automatic `time_bucket` downsampling, runtime palette switch.
- [x] **Phase 5** — Alerting: threshold rules engine, per-host evaluation (fires when a metric breaches for the whole duration window, resolves when it clears), Telegram + email channels, alerts board UI.
- [x] **Phase 6** — Hardening: **JWT auth** (single-operator login, Bearer-protected dashboard, SSE token via query string), retention policies, OpenTelemetry (traces + metrics), Problem Details exception handler, per-IP rate limiting.
- [x] **Phase 7** — Infrastructure inventory: servers (provider, IP, specs, monthly cost, renewal date, user count), directed topology links, CRUD, billing countdown on the dashboard.
- [x] **Phase 8** — First-run setup: create-admin onboarding (operator stored in the DB), no shipped default password, auto-generated persisted JWT secret.
- [x] **Phase 9** — Agent auto-discovery: the agent reports OS / cores / RAM / disk / uptime / listening ports; the matching server's discovered fields upsert (manual cost & renewal preserved).
- [x] **Phase 11** — Visual topology graph: dependency-free SVG of server connections, coloured by liveness.
- [ ] **Phase 10 / 12+ (planned)** — users/sessions/VPN-peer metric · SSH-based discovery for agentless servers · remote agents over HTTPS · renewal-due alerts · richer cost dashboard.

---

## Tests

```bash
dotnet test Heimdall.slnx
```

- **`Heimdall.UnitTests`** — Domain invariants (Result, value objects, aggregates) and Application handlers (NSubstitute fakes, deterministic `TimeProvider`).
- **`Heimdall.ArchitectureTests`** — NetArchTest layering: Domain depends on nothing outward, Application never touches Infrastructure/Api, handlers live in Application.
- **`Heimdall.IntegrationTests`** — real TimescaleDB via Testcontainers: repository round-trips for hosts, metrics, health-checks, and alert rules/alerts. Requires Docker.

## Engineering notes

- **Storage** uses Npgsql + Dapper + an idempotent SQL initializer rather than EF Core migrations — avoids EF-provider churn on the preview SDK and matches the time-series access pattern (binary `COPY`, raw `create_hypertable`). EF Core will be introduced for the relational side (alert rules, dashboards) as that domain grows.
- **Web** uses npm (not pnpm) because `corepack` could not enable pnpm without elevated permissions on this machine.
- **.NET Aspire** orchestration is deferred; local dev uses `docker compose` for TimescaleDB to keep the build clean on the preview SDK.
- **Agent resilience:** the agent buffers batches (bounded, ~10 min) and resends oldest-first, so a server restart or network blip doesn't lose samples; its HTTP timeout is capped so a hung server can't stall the collection loop.
- **Desktop deployment:** the agent must run natively on the host (a container would report the container's metrics, not the host's), so the deploy model is published API+Agent (`dist/`) + `next start` web + TimescaleDB in Docker, wired by `scripts/*.ps1` and a Desktop shortcut. The API port is pinned in `appsettings.json` (`Urls`) since `launchSettings.json` isn't published.
- **JWT auth (single operator):** on first run the dashboard calls `/api/auth/status`, shows a **create-admin** screen, and `POST /api/auth/setup` stores the operator (username + SHA-256 hash) in the `app_config` table — so no default password ships. `/api/auth/login` issues an HS256 Bearer token (8h, configurable) signed with a secret that's generated once and persisted under `%LOCALAPPDATA%/Heimdall/jwt.secret` (or pinned via `Heimdall:Jwt:Secret`). Read/admin endpoints require the token; SSE receives it via `?access_token=` (since `EventSource` can't set headers). Agent endpoints stay on enrollment/agent-key auth. Simplified for a single-user pet tool: **no refresh-token rotation** (re-login on expiry).
- **Auto-discovery:** the agent posts a host inventory snapshot (OS, cores, RAM, disk, uptime, listening TCP ports) to `/api/ingest/inventory` on its first tick and hourly; the server matched by linked host name (else name) has its **discovered** fields upserted while manual fields (cost, renewal date, notes, role, links) are preserved — you only type what the machine can't know.
