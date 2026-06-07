import { clearToken, getToken } from "./auth";

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5087";

export const apiBase = API_BASE;

/** fetch that attaches the bearer token and bounces to /login on 401. */
async function authFetch(url: string, init: RequestInit = {}): Promise<Response> {
  const token = getToken();
  const headers = new Headers(init.headers);
  if (token) headers.set("Authorization", `Bearer ${token}`);

  const response = await fetch(url, { ...init, headers, cache: "no-store" });
  if (response.status === 401 && typeof window !== "undefined") {
    clearToken();
    if (!window.location.pathname.startsWith("/login")) window.location.href = "/login";
  }
  return response;
}

export interface MetricPoint {
  timestampUnixMs: number;
  value: number;
}

export interface MetricSeries {
  metric: string;
  points: MetricPoint[];
}

export interface HostMetrics {
  hostName: string;
  series: MetricSeries[];
}

export interface HealthCheckStatus {
  id: string;
  name: string;
  kind: string;
  target: string;
  enabled: boolean;
  isUp: boolean | null;
  latencyMs: number | null;
  lastCheckedAtUnixMs: number | null;
}

export interface OverviewHost {
  id: string;
  name: string;
  lastSeenAtUnixMs: number;
  online: boolean;
  cpu: number | null;
  memory: number | null;
  disk: number | null;
}

export interface OverviewSnapshot {
  generatedAtUnixMs: number;
  hosts: OverviewHost[];
  healthChecks: HealthCheckStatus[];
}

export async function fetchOverview(signal?: AbortSignal): Promise<OverviewSnapshot> {
  const response = await authFetch(`${API_BASE}/api/overview`, { signal });
  if (!response.ok) throw new Error(`Failed to load overview (${response.status})`);
  return response.json();
}

export async function fetchHostMetrics(
  hostName: string,
  minutes: number,
  maxPoints: number,
  names: string[] = ["cpu.usage", "memory.usage"],
  signal?: AbortSignal,
): Promise<HostMetrics> {
  const query = new URLSearchParams({ names: names.join(","), minutes: String(minutes), maxPoints: String(maxPoints) });
  const response = await authFetch(`${API_BASE}/api/hosts/${encodeURIComponent(hostName)}/metrics?${query}`, { signal });
  if (!response.ok) throw new Error(`Failed to load metrics (${response.status})`);
  return response.json();
}

export interface AlertRule {
  id: string;
  name: string;
  metric: string;
  operator: string;
  threshold: number;
  durationSeconds: number;
  severity: string;
  hostName: string | null;
  enabled: boolean;
}

export interface Alert {
  id: string;
  ruleId: string;
  ruleName: string;
  hostName: string;
  metric: string;
  severity: string;
  status: string;
  value: number;
  startedAtUnixMs: number;
  resolvedAtUnixMs: number | null;
}

export interface CreateAlertRuleInput {
  name: string;
  metric: string;
  operator: string;
  threshold: number;
  durationSeconds: number;
  severity: string;
  hostName?: string | null;
}

export async function fetchAlerts(signal?: AbortSignal): Promise<Alert[]> {
  const response = await authFetch(`${API_BASE}/api/alerts`, { signal });
  if (!response.ok) throw new Error(`Failed to load alerts (${response.status})`);
  return response.json();
}

export async function fetchAlertRules(signal?: AbortSignal): Promise<AlertRule[]> {
  const response = await authFetch(`${API_BASE}/api/alerts/rules`, { signal });
  if (!response.ok) throw new Error(`Failed to load rules (${response.status})`);
  return response.json();
}

export async function createAlertRule(input: CreateAlertRuleInput): Promise<boolean> {
  const response = await authFetch(`${API_BASE}/api/alerts/rules`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return response.ok;
}

export async function deleteAlertRule(id: string): Promise<void> {
  await authFetch(`${API_BASE}/api/alerts/rules/${id}`, { method: "DELETE" });
}

export interface CreateHealthCheckInput {
  name: string;
  kind: string; // "Http" | "Tcp"
  target: string; // URL for Http, "host:port" for Tcp
  intervalSeconds: number;
}

export async function createHealthCheck(input: CreateHealthCheckInput): Promise<boolean> {
  const response = await authFetch(`${API_BASE}/api/healthchecks`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return response.ok;
}

export async function deleteHealthCheck(id: string): Promise<void> {
  await authFetch(`${API_BASE}/api/healthchecks/${id}`, { method: "DELETE" });
}
