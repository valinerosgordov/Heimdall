# Heimdall

Self-hosted monitoring for your servers and projects — a focused mix of Prometheus (collection) and Grafana (visualization), built as one coherent stack. The watchman of your infrastructure.

> **Status:** Phases 0–6 complete. A cross-platform `.NET 10` agent enrolls with a per-host key and pushes CPU/memory/disk/network/uptime to the API, which stores samples in TimescaleDB. A scheduler probes HTTP/TCP health-check targets, and a threshold **alert engine** fires/resolves rules with Telegram + email channels. The Next.js dashboard (ASUS ROG dark HUD) shows a **live overview grid via Server-Sent Events**, per-host detail with radial gauges and **auto-downsampled** time-series, and an alerts board. Hardened with a **JWT-authenticated dashboard, per-IP rate limiting, OpenTelemetry traces/metrics, retention policies, and Problem Details** exception handling. **91 tests** (unit + architecture + Testcontainers integration) pass.

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
| `Heimdall.Domain` | SharedKernel (`Result<T>`, `Error`, `Entity`), Hosts + Metrics domain (value objects, invariants) |
| `Heimdall.Application` | Explicit handlers (Ingest / QueryHostMetrics / ListHosts), repository abstractions. No MediatR. Depends only on Domain + Contracts. |
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

Open <http://localhost:3100> and log in (**admin / heimdall** in dev). Your host appears within a few seconds, with live charts. The web uses a dedicated port (3100) to avoid colliding with other local dev servers.

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
3. shows the dashboard in a native window — log in with **admin / heimdall**.

**Closing the window stops everything** (API, agent, TimescaleDB; data is preserved). The agent runs natively, so it measures *this machine* (a container would report the container's metrics).

Prefer a browser to the app window? `scripts\start-heimdall.ps1` runs the same backend and opens <http://localhost:5087>.

### Sharing the app with others

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1   # -> release\Heimdall-<timestamp>.zip
```

This produces a **self-contained** zip (no .NET install needed on the target). The recipient unzips it and double-clicks **`Run Heimdall.bat`**. Requirements on their machine:

- **Docker Desktop** (running) — Heimdall stores metrics in TimescaleDB, which runs in Docker. This is the one hard dependency.
- **WebView2 Runtime** — preinstalled on Windows 11; on Windows 10 it's a quick install.

The build ships throwaway dev credentials (`admin` / `heimdall`) and a default signing secret — fine for trying it locally, but **change them before exposing Heimdall to a network**.

---

## API

Dashboard read/admin endpoints (`/api/hosts`, `/api/overview`, `/api/stream/*`, `/api/healthchecks`, `/api/alerts*`) require a JWT `Bearer` token; agent endpoints (`/api/enroll`, `/api/ingest/*`) use key auth.

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `GET` | `/health` | — | Liveness |
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

- **API** — `src/Heimdall.Api/appsettings.Development.json`: connection string, CORS origins, the dev `Heimdall:EnrollmentKey`, the operator account (`Heimdall:Auth:Username` + `Heimdall:Auth:PasswordSha256`) and JWT secret (`Heimdall:Jwt:Secret`). **Dev login: `admin` / `heimdall`.**
- **Agent** — `src/Heimdall.Agent/appsettings.json`: `ServerUrl`, `EnrollmentKey`, optional `HostName` (defaults to the machine name), `IntervalSeconds`. On first run the agent enrolls and caches its per-host key under `%LOCALAPPDATA%/Heimdall/<host>.key` (Linux: `~/.local/share/Heimdall/`).
- **Web** — `src/Heimdall.Web/.env.local`: `NEXT_PUBLIC_API_URL`.
- **Alert channels (optional)** — `Heimdall:Telegram:{BotToken,ChatId}` and/or `Heimdall:Email:{Host,Port,From,To,User,Password}`. Unconfigured channels are skipped silently.
- **Telemetry** — OpenTelemetry traces + metrics export to the console by default; set up an OTLP collector and swap `AddConsoleExporter()` for `AddOtlpExporter()` for Jaeger/Seq.

> `appsettings.Development.json` is **gitignored** because it holds the dev JWT secret, operator hash and enrollment key. On a fresh clone, copy the template first:
> ```bash
> cp src/Heimdall.Api/appsettings.Development.json.example src/Heimdall.Api/appsettings.Development.json
> ```
> The shipped values are **dev-only throwaways**. In production use User Secrets / Key Vault — never commit secrets.

---

## Roadmap

- [x] **Phase 0** — Foundation: solution, SharedKernel, Timescale, schema initializer.
- [x] **Phase 1** — Tracer bullet: agent → ingest → store → query → live chart.
- [x] **Phase 2** — Full collection (CPU/memory/disk/network/uptime, Windows + Linux agent) + host enrollment with per-agent keys.
- [x] **Phase 3** — Health-checks: HTTP/TCP probe scheduler, targets CRUD, status board + history.
- [x] **Phase 4** — Dashboards: live overview grid (SSE) + health board, host detail (radial gauges + ranged time-series), automatic `time_bucket` downsampling, runtime palette switch.
- [x] **Phase 5** — Alerting: threshold rules engine, per-host evaluation (fires when a metric breaches for the whole duration window, resolves when it clears), Telegram + email channels, alerts board UI.
- [x] **Phase 6** — Hardening: **JWT auth** (single-operator login, Bearer-protected dashboard, SSE token via query string), retention policies, OpenTelemetry (traces + metrics), Problem Details exception handler, per-IP rate limiting.

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
- **JWT auth (single operator):** the dashboard logs in via `/api/auth/login` for an HS256 Bearer token (8h, configurable); read/admin endpoints require it, and SSE receives it via `?access_token=` (since `EventSource` can't set headers). Agent endpoints stay on enrollment/agent-key auth. Simplified for a single-user pet tool: **no refresh-token rotation** (re-login on expiry) and credentials live in config (`Heimdall:Auth:Username` + `PasswordSha256`).
