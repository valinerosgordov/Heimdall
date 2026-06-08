"use client";

import { useEffect, useMemo, useState } from "react";
import { ServerDto, fetchInventory } from "@/lib/api";

function SectionTitle({ children }: { children: React.ReactNode }) {
  return <h2 className="mb-3 font-heading text-sm font-semibold uppercase tracking-[0.14em] text-faint">{children}</h2>;
}

const CYCLE_MONTHS: Record<string, number> = { monthly: 1, quarterly: 3, yearly: 12 };
const cycleMonths = (c: string | null) => CYCLE_MONTHS[c ?? "monthly"] ?? 1;
const fmt = (n: number) => n.toLocaleString("en-US");

function dueColor(days: number | null): string {
  if (days === null) return "var(--muted)";
  if (days < 0) return "var(--danger)";
  if (days <= 7) return "var(--warning)";
  return "var(--ink)";
}

export default function BillingPage() {
  const [servers, setServers] = useState<ServerDto[]>([]);

  const refresh = async () => {
    try {
      setServers((await fetchInventory()).servers);
    } catch {
      /* keep last good state */
    }
  };

  useEffect(() => {
    void refresh();
    const id = setInterval(refresh, 10000);
    return () => clearInterval(id);
  }, []);

  const totals = useMemo(() => {
    const m = new Map<string, number>();
    for (const s of servers) {
      if (s.monthlyCost) {
        const cur = s.currency ?? "?";
        m.set(cur, (m.get(cur) ?? 0) + s.monthlyCost);
      }
    }
    return [...m.entries()];
  }, [servers]);

  const byProvider = useMemo(() => {
    const m = new Map<string, Map<string, number>>();
    for (const s of servers) {
      if (!s.monthlyCost) continue;
      const provider = s.provider ?? "—";
      const cur = s.currency ?? "?";
      if (!m.has(provider)) m.set(provider, new Map());
      const cm = m.get(provider)!;
      cm.set(cur, (cm.get(cur) ?? 0) + s.monthlyCost);
    }
    return [...m.entries()].sort((a, b) => a[0].localeCompare(b[0]));
  }, [servers]);

  const renewals = useMemo(
    () =>
      servers
        .filter((s) => s.paidUntil && s.daysUntilDue !== null)
        .sort((a, b) => (a.daysUntilDue ?? 0) - (b.daysUntilDue ?? 0)),
    [servers],
  );

  const autoRenewing = servers.filter((s) => s.autoRenew).length;

  return (
    <div className="space-y-10">
      <section>
        <SectionTitle>Spend</SectionTitle>
        {totals.length === 0 ? (
          <p className="text-sm text-muted">No costs entered yet — add monthly cost on a server in the Infra tab.</p>
        ) : (
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            {totals.map(([cur, sum]) => (
              <div key={cur} className="hud-panel p-3">
                <div className="text-[10px] uppercase tracking-widest text-faint">{cur} / month</div>
                <div className="font-mono text-xl text-ink">
                  {fmt(sum)} {cur}
                </div>
                <div className="font-mono text-[10px] text-faint">
                  ≈ {fmt(sum * 12)} {cur} / yr
                </div>
              </div>
            ))}
            <div className="hud-panel p-3">
              <div className="text-[10px] uppercase tracking-widest text-faint">Auto-renewing</div>
              <div className="font-mono text-xl text-ink">
                {autoRenewing}
                <span className="text-faint">/{servers.length}</span>
              </div>
            </div>
          </div>
        )}
      </section>

      <section>
        <SectionTitle>By provider</SectionTitle>
        <div className="hud-panel p-4">
          {byProvider.length === 0 ? (
            <p className="text-sm text-muted">—</p>
          ) : (
            <ul className="space-y-1.5">
              {byProvider.map(([provider, cm]) => (
                <li key={provider} className="flex items-center justify-between gap-4 font-mono text-xs">
                  <span className="truncate text-ink">{provider}</span>
                  <span className="shrink-0 text-muted">
                    {[...cm.entries()].map(([c, s]) => `${fmt(s)} ${c}/mo`).join(" · ")}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>

      <section>
        <SectionTitle>Upcoming renewals</SectionTitle>
        <div className="hud-panel overflow-hidden">
          <div className="grid grid-cols-[1fr_auto_auto_auto] gap-4 border-b border-border px-4 py-2 text-[10px] uppercase tracking-widest text-faint">
            <span>Server</span>
            <span>Amount</span>
            <span>Date</span>
            <span>Due</span>
          </div>
          {renewals.length === 0 ? (
            <p className="px-4 py-3 text-sm text-muted">No renewal dates set.</p>
          ) : (
            renewals.map((s) => {
              const amount = s.monthlyCost ? s.monthlyCost * cycleMonths(s.billingCycle) : null;
              return (
                <div
                  key={s.id}
                  className="grid grid-cols-[1fr_auto_auto_auto] items-center gap-4 border-b border-border px-4 py-2.5 text-xs last:border-b-0"
                  data-status={s.daysUntilDue !== null && s.daysUntilDue < 0 ? "down" : undefined}
                >
                  <span className="truncate font-heading text-ink">
                    {s.name}
                    {s.autoRenew && (
                      <span className="text-success" title="auto-renews">
                        {" "}
                        ⟳
                      </span>
                    )}
                  </span>
                  <span className="font-mono text-muted">
                    {amount !== null ? `${fmt(amount)} ${s.currency ?? ""} / ${s.billingCycle ?? "mo"}` : "—"}
                  </span>
                  <span className="font-mono text-faint">{s.paidUntil}</span>
                  <span className="font-mono font-semibold" style={{ color: dueColor(s.daysUntilDue) }}>
                    {s.daysUntilDue === null
                      ? "—"
                      : s.daysUntilDue < 0
                        ? `overdue ${Math.abs(s.daysUntilDue)}d`
                        : `${s.daysUntilDue}d`}
                  </span>
                </div>
              );
            })
          )}
        </div>
      </section>
    </div>
  );
}
