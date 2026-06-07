"use client";

import { useEffect, useState } from "react";
import {
  Alert,
  AlertRule,
  CreateAlertRuleInput,
  createAlertRule,
  deleteAlertRule,
  fetchAlertRules,
  fetchAlerts,
} from "@/lib/api";

function severityColor(severity: string): string {
  return severity === "critical" ? "var(--danger)" : "var(--warning)";
}

function time(unixMs: number): string {
  return new Date(unixMs).toLocaleTimeString("en-GB");
}

function SectionTitle({ children }: { children: React.ReactNode }) {
  return <h2 className="mb-3 font-heading text-sm font-semibold uppercase tracking-[0.14em] text-faint">{children}</h2>;
}

function AlertRow({ alert }: { alert: Alert }) {
  const firing = alert.status === "firing";
  return (
    <div
      className="grid grid-cols-[3px_1fr_auto] items-center gap-3 border-b border-border last:border-b-0"
      data-status={firing && alert.severity === "critical" ? "down" : undefined}
    >
      <div className="h-full min-h-[3rem] w-[3px]" style={{ background: severityColor(alert.severity) }} />
      <div className="min-w-0 py-2">
        <div className="flex items-center gap-2">
          <span className="font-heading text-sm font-semibold text-ink">{alert.ruleName}</span>
          <span
            className="text-[10px] font-semibold uppercase tracking-wider"
            style={{ color: severityColor(alert.severity), animation: firing && alert.severity === "critical" ? "var(--animate-pulse-glow)" : undefined }}
          >
            {alert.severity}
          </span>
        </div>
        <div className="truncate font-mono text-xs text-muted">
          {alert.hostName} · {alert.metric} = {alert.value.toFixed(1)}
        </div>
      </div>
      <div className="py-2 text-right font-mono text-xs text-muted">
        <div>{time(alert.startedAtUnixMs)}</div>
        {alert.resolvedAtUnixMs && <div className="text-faint">→ {time(alert.resolvedAtUnixMs)}</div>}
      </div>
    </div>
  );
}

const EMPTY_FORM: CreateAlertRuleInput = {
  name: "",
  metric: "cpu.usage",
  operator: "gt",
  threshold: 90,
  durationSeconds: 60,
  severity: "warning",
  hostName: "",
};

export default function AlertsPage() {
  const [alerts, setAlerts] = useState<Alert[]>([]);
  const [rules, setRules] = useState<AlertRule[]>([]);
  const [form, setForm] = useState<CreateAlertRuleInput>(EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);

  const refresh = async () => {
    try {
      const [a, r] = await Promise.all([fetchAlerts(), fetchAlertRules()]);
      setAlerts(a);
      setRules(r);
    } catch {
      /* keep last good state */
    }
  };

  useEffect(() => {
    void refresh();
    const id = setInterval(refresh, 3000);
    return () => clearInterval(id);
  }, []);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    const ok = await createAlertRule({ ...form, hostName: form.hostName?.trim() ? form.hostName : null });
    if (!ok) {
      setError("Could not create rule — check the values.");
      return;
    }
    setForm(EMPTY_FORM);
    await refresh();
  };

  const remove = async (id: string) => {
    await deleteAlertRule(id);
    await refresh();
  };

  const active = alerts.filter((a) => a.status === "firing");
  const history = alerts.filter((a) => a.status === "resolved");

  return (
    <div className="space-y-10">
      <section>
        <SectionTitle>Active Alerts</SectionTitle>
        {active.length === 0 ? (
          <p className="text-sm text-muted">No active alerts. All systems nominal.</p>
        ) : (
          <div className="hud-panel">{active.map((a) => <AlertRow key={a.id} alert={a} />)}</div>
        )}
      </section>

      <section className="grid gap-6 lg:grid-cols-2">
        <div>
          <SectionTitle>Alert Rules</SectionTitle>
          <div className="hud-panel p-4">
            {rules.length === 0 ? (
              <p className="mb-4 text-sm text-muted">No rules yet.</p>
            ) : (
              <ul className="mb-4 space-y-2">
                {rules.map((r) => (
                  <li key={r.id} className="flex items-center justify-between gap-2 border-b border-border pb-2 last:border-b-0">
                    <span className="min-w-0">
                      <span className="font-heading text-sm text-ink">{r.name}</span>
                      <span className="ml-2 font-mono text-xs text-muted">
                        {r.metric} {r.operator} {r.threshold} · {r.durationSeconds}s · {r.hostName ?? "all hosts"}
                      </span>
                    </span>
                    <button onClick={() => remove(r.id)} className="text-xs uppercase tracking-wider text-faint hover:text-danger">
                      delete
                    </button>
                  </li>
                ))}
              </ul>
            )}

            <form onSubmit={submit} className="grid grid-cols-2 gap-2 text-sm">
              <input required placeholder="Rule name" value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })} className="col-span-2 bg-elevated px-2 py-1.5 text-ink outline-none" />
              <input placeholder="metric" value={form.metric}
                onChange={(e) => setForm({ ...form, metric: e.target.value })} className="bg-elevated px-2 py-1.5 font-mono text-ink outline-none" />
              <select value={form.operator} onChange={(e) => setForm({ ...form, operator: e.target.value })} className="bg-elevated px-2 py-1.5 text-ink outline-none">
                <option value="gt">&gt;</option>
                <option value="gte">&ge;</option>
                <option value="lt">&lt;</option>
                <option value="lte">&le;</option>
              </select>
              <input type="number" step="any" placeholder="threshold" value={form.threshold}
                onChange={(e) => setForm({ ...form, threshold: Number(e.target.value) })} className="bg-elevated px-2 py-1.5 font-mono text-ink outline-none" />
              <input type="number" placeholder="duration (s)" value={form.durationSeconds}
                onChange={(e) => setForm({ ...form, durationSeconds: Number(e.target.value) })} className="bg-elevated px-2 py-1.5 font-mono text-ink outline-none" />
              <select value={form.severity} onChange={(e) => setForm({ ...form, severity: e.target.value })} className="bg-elevated px-2 py-1.5 text-ink outline-none">
                <option value="warning">warning</option>
                <option value="critical">critical</option>
              </select>
              <input placeholder="host (optional)" value={form.hostName ?? ""}
                onChange={(e) => setForm({ ...form, hostName: e.target.value })} className="bg-elevated px-2 py-1.5 text-ink outline-none" />
              <button type="submit" className="col-span-2 mt-1 border border-accent px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent hover:bg-elevated" style={{ borderRadius: "var(--radius-md)" }}>
                Add rule
              </button>
              {error && <p className="col-span-2 text-xs text-danger">{error}</p>}
            </form>
          </div>
        </div>

        <div>
          <SectionTitle>History</SectionTitle>
          {history.length === 0 ? (
            <p className="text-sm text-muted">No resolved alerts.</p>
          ) : (
            <div className="hud-panel">{history.map((a) => <AlertRow key={a.id} alert={a} />)}</div>
          )}
        </div>
      </section>
    </div>
  );
}
