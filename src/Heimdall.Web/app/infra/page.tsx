"use client";

import { useEffect, useMemo, useState } from "react";
import StatusPill, { StatusState } from "@/components/StatusPill";
import TopologyGraph from "@/components/TopologyGraph";
import {
  CreateServerInput,
  HealthCheckStatus,
  OverviewHost,
  ServerDto,
  ServerLinkDto,
  createServer,
  createServerLink,
  deleteServer,
  deleteServerLink,
  fetchHealthChecks,
  fetchInventory,
  fetchOverview,
  updateServer,
} from "@/lib/api";

function SectionTitle({ children }: { children: React.ReactNode }) {
  return <h2 className="mb-3 font-heading text-sm font-semibold uppercase tracking-[0.14em] text-faint">{children}</h2>;
}

function statusOf(isUp: boolean | null): StatusState {
  if (isUp === null) return "idle";
  return isUp ? "up" : "down";
}

function DueBadge({ days }: { days: number | null }) {
  if (days === null) return <span className="font-mono text-xs text-faint">no date</span>;
  let color = "var(--muted)";
  let label = `in ${days}d`;
  if (days < 0) {
    color = "var(--danger)";
    label = `overdue ${Math.abs(days)}d`;
  } else if (days === 0) {
    color = "var(--danger)";
    label = "due today";
  } else if (days <= 7) {
    color = "var(--warning)";
    label = `due in ${days}d`;
  }
  return (
    <span className="font-mono text-xs font-semibold" style={{ color }}>
      {label}
    </span>
  );
}

function timeAgo(unixMs: number): string {
  const s = Math.max(0, Math.floor((Date.now() - unixMs) / 1000));
  if (s < 60) return `${s}s ago`;
  if (s < 3600) return `${Math.floor(s / 60)}m ago`;
  if (s < 86400) return `${Math.floor(s / 3600)}h ago`;
  return `${Math.floor(s / 86400)}d ago`;
}

function Field({ label, value, className = "" }: { label: string; value?: string | null; className?: string }) {
  return (
    <div className={className}>
      <span className="text-faint">{label}: </span>
      <span className="text-ink">{value && value.length > 0 ? value : "—"}</span>
    </div>
  );
}

function specsText(s: ServerDto): string {
  const parts = [
    s.cpuCores ? `${s.cpuCores} vCPU` : null,
    s.ramGb ? `${s.ramGb} GB` : null,
    s.diskGb ? `${s.diskGb} GB disk` : null,
  ].filter(Boolean);
  return parts.join(" / ");
}

function costText(s: ServerDto): string {
  if (s.monthlyCost === null) return "";
  return `${s.monthlyCost} ${s.currency ?? ""}/mo`.trim();
}

const EMPTY: CreateServerInput = { name: "", currency: "RUB" };

export default function InfraPage() {
  const [servers, setServers] = useState<ServerDto[]>([]);
  const [links, setLinks] = useState<ServerLinkDto[]>([]);
  const [checks, setChecks] = useState<HealthCheckStatus[]>([]);
  const [hosts, setHosts] = useState<OverviewHost[]>([]);
  const [form, setForm] = useState<CreateServerInput>(EMPTY);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [linkForm, setLinkForm] = useState({ fromServerId: "", toServerId: "", kind: "proxy" });

  const refresh = async () => {
    try {
      const [inv, hc, ov] = await Promise.all([fetchInventory(), fetchHealthChecks(), fetchOverview()]);
      setServers(inv.servers);
      setLinks(inv.links);
      setChecks(hc);
      setHosts(ov.hosts);
    } catch {
      /* keep last good state */
    }
  };

  useEffect(() => {
    void refresh();
    const id = setInterval(refresh, 5000);
    return () => clearInterval(id);
  }, []);

  const nameById = useMemo(() => new Map(servers.map((s) => [s.id, s.name])), [servers]);
  const hostByName = useMemo(() => new Map(hosts.map((h) => [h.name.toLowerCase(), h])), [hosts]);
  const liveHostFor = (s: ServerDto) => hostByName.get((s.linkedHostName ?? s.name).toLowerCase());

  const totalsByCurrency = useMemo(() => {
    const m = new Map<string, number>();
    for (const s of servers) {
      if (s.monthlyCost) {
        const cur = s.currency ?? "?";
        m.set(cur, (m.get(cur) ?? 0) + s.monthlyCost);
      }
    }
    return [...m.entries()];
  }, [servers]);

  const dueSoon = servers.filter((s) => s.daysUntilDue !== null && s.daysUntilDue <= 7).length;
  const totalUsers = servers.reduce((acc, s) => acc + (s.userCount ?? 0), 0);

  const setNum = (key: keyof CreateServerInput, value: string) =>
    setForm({ ...form, [key]: value === "" ? null : Number(value) });

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    const ok = editingId ? await updateServer(editingId, form) : await createServer(form);
    if (!ok) {
      setError("Could not save — check the values (name is required, date as yyyy-MM-dd).");
      return;
    }
    setForm(EMPTY);
    setEditingId(null);
    setShowForm(false);
    await refresh();
  };

  const startEdit = (s: ServerDto) => {
    setEditingId(s.id);
    setForm({
      name: s.name,
      provider: s.provider,
      ipAddress: s.ipAddress,
      hostname: s.hostname,
      role: s.role,
      cpuCores: s.cpuCores,
      ramGb: s.ramGb,
      diskGb: s.diskGb,
      location: s.location,
      monthlyCost: s.monthlyCost,
      currency: s.currency ?? "RUB",
      paidUntil: s.paidUntil,
      userCount: s.userCount,
      notes: s.notes,
      linkedHealthCheckId: s.linkedHealthCheckId,
      linkedHostName: s.linkedHostName,
    });
    setShowForm(true);
    setError(null);
  };

  const startAdd = () => {
    setEditingId(null);
    setForm(EMPTY);
    setShowForm(true);
    setError(null);
  };

  const remove = async (id: string) => {
    await deleteServer(id);
    await refresh();
  };

  const addLink = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!linkForm.fromServerId || !linkForm.toServerId) return;
    await createServerLink(linkForm);
    setLinkForm({ fromServerId: "", toServerId: "", kind: "proxy" });
    await refresh();
  };

  const removeLink = async (id: string) => {
    await deleteServerLink(id);
    await refresh();
  };

  const inputClass = "bg-elevated px-2 py-1.5 text-ink outline-none";

  return (
    <div className="space-y-10">
      <section>
        <div className="mb-3 flex items-center justify-between">
          <SectionTitle>Infrastructure</SectionTitle>
          <button
            onClick={showForm ? () => setShowForm(false) : startAdd}
            className="border border-accent px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent hover:bg-elevated"
            style={{ borderRadius: "var(--radius-md)" }}
          >
            {showForm ? "Close" : "Add server"}
          </button>
        </div>

        <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div className="hud-panel p-3">
            <div className="text-[10px] uppercase tracking-widest text-faint">Servers</div>
            <div className="font-mono text-xl text-ink">{servers.length}</div>
          </div>
          <div className="hud-panel p-3">
            <div className="text-[10px] uppercase tracking-widest text-faint">Cost / month</div>
            <div className="font-mono text-sm text-ink">
              {totalsByCurrency.length === 0
                ? "—"
                : totalsByCurrency.map(([cur, sum]) => `${sum} ${cur}`).join(" · ")}
            </div>
            {totalsByCurrency.length > 0 && (
              <div className="font-mono text-[10px] text-faint">
                ≈ {totalsByCurrency.map(([cur, sum]) => `${sum * 12} ${cur}`).join(" · ")} / yr
              </div>
            )}
          </div>
          <div className="hud-panel p-3">
            <div className="text-[10px] uppercase tracking-widest text-faint">Due ≤ 7d</div>
            <div className="font-mono text-xl" style={{ color: dueSoon > 0 ? "var(--warning)" : "var(--ink)" }}>
              {dueSoon}
            </div>
          </div>
          <div className="hud-panel p-3">
            <div className="text-[10px] uppercase tracking-widest text-faint">Users</div>
            <div className="font-mono text-xl text-ink">{totalUsers}</div>
          </div>
        </div>

        {showForm && (
          <form onSubmit={submit} className="hud-panel mb-4 grid grid-cols-2 gap-2 p-4 text-sm sm:grid-cols-4">
            <input required placeholder="Name *" value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })} className={`col-span-2 ${inputClass}`} />
            <input placeholder="Provider" value={form.provider ?? ""}
              onChange={(e) => setForm({ ...form, provider: e.target.value })} className={inputClass} />
            <input placeholder="Location" value={form.location ?? ""}
              onChange={(e) => setForm({ ...form, location: e.target.value })} className={inputClass} />
            <input placeholder="IP address" value={form.ipAddress ?? ""}
              onChange={(e) => setForm({ ...form, ipAddress: e.target.value })} className={`font-mono ${inputClass}`} />
            <input placeholder="Hostname" value={form.hostname ?? ""}
              onChange={(e) => setForm({ ...form, hostname: e.target.value })} className={`font-mono ${inputClass}`} />
            <input placeholder="Role / purpose" value={form.role ?? ""}
              onChange={(e) => setForm({ ...form, role: e.target.value })} className={`col-span-2 ${inputClass}`} />
            <input type="number" step="1" min="0" placeholder="vCPU" value={form.cpuCores ?? ""}
              onChange={(e) => setNum("cpuCores", e.target.value)} className={`font-mono ${inputClass}`} />
            <input type="number" step="any" placeholder="RAM GB" value={form.ramGb ?? ""}
              onChange={(e) => setNum("ramGb", e.target.value)} className={`font-mono ${inputClass}`} />
            <input type="number" step="any" placeholder="Disk GB" value={form.diskGb ?? ""}
              onChange={(e) => setNum("diskGb", e.target.value)} className={`font-mono ${inputClass}`} />
            <input type="number" step="1" min="0" placeholder="Users" value={form.userCount ?? ""}
              onChange={(e) => setNum("userCount", e.target.value)} className={`font-mono ${inputClass}`} />
            <input type="number" step="any" placeholder="Cost / mo" value={form.monthlyCost ?? ""}
              onChange={(e) => setNum("monthlyCost", e.target.value)} className={`font-mono ${inputClass}`} />
            <input placeholder="Currency" value={form.currency ?? ""}
              onChange={(e) => setForm({ ...form, currency: e.target.value })} className={`font-mono ${inputClass}`} />
            <label className="flex flex-col gap-1 text-[10px] uppercase tracking-widest text-faint">
              Paid until
              <input type="date" value={form.paidUntil ?? ""}
                onChange={(e) => setForm({ ...form, paidUntil: e.target.value })} className={`font-mono ${inputClass}`} />
            </label>
            <select value={form.linkedHealthCheckId ?? ""}
              onChange={(e) => setForm({ ...form, linkedHealthCheckId: e.target.value || null })}
              className={`col-span-2 self-end ${inputClass}`}>
              <option value="">— link a monitor (status) —</option>
              {checks.map((c) => (
                <option key={c.id} value={c.id}>{c.name} ({c.kind})</option>
              ))}
            </select>
            <textarea placeholder="Notes" value={form.notes ?? ""}
              onChange={(e) => setForm({ ...form, notes: e.target.value })} className={`col-span-2 sm:col-span-4 ${inputClass}`} rows={2} />
            <button type="submit"
              className="col-span-2 border border-accent px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent hover:bg-elevated sm:col-span-4"
              style={{ borderRadius: "var(--radius-md)" }}>
              {editingId ? "Save changes" : "Add server"}
            </button>
            {error && <p className="col-span-2 text-xs text-danger sm:col-span-4">{error}</p>}
          </form>
        )}

        {servers.length === 0 ? (
          <p className="text-sm text-muted">No servers yet — add one above. Billing data is entered manually.</p>
        ) : (
          <div className="grid gap-4 md:grid-cols-2">
            {servers.map((s) => (
              <div key={s.id} className="hud-panel p-4" data-status={s.isUp === false ? "down" : undefined}>
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="truncate font-heading text-base font-semibold text-ink">{s.name}</span>
                      {s.linkedHealthCheckId && <StatusPill state={statusOf(s.isUp)} />}
                    </div>
                    <div className="truncate font-mono text-xs text-muted">
                      {[s.provider, s.location].filter(Boolean).join(" · ") || "—"}
                    </div>
                  </div>
                  <div className="flex shrink-0 gap-3 text-xs uppercase tracking-wider">
                    <button onClick={() => startEdit(s)} className="text-faint hover:text-accent">edit</button>
                    <button onClick={() => remove(s.id)} className="text-faint hover:text-danger">del</button>
                  </div>
                </div>

                <div className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1 font-mono text-xs">
                  <Field label="IP" value={s.ipAddress} />
                  <Field label="Host" value={s.hostname} />
                  <Field label="Specs" value={specsText(s)} />
                  <Field label="Users" value={s.userCount?.toString()} />
                  <Field label="Role" value={s.role} className="col-span-2" />
                  {s.os && <Field label="OS" value={s.os} className="col-span-2" />}
                  {s.listeningPorts && <Field label="Ports" value={s.listeningPorts} className="col-span-2" />}
                </div>

                {(() => {
                  const h = liveHostFor(s);
                  return h?.online ? (
                    <div className="mt-2 flex items-center gap-3 font-mono text-xs">
                      <span className="text-[10px] uppercase tracking-widest" style={{ color: "var(--success)" }}>● live</span>
                      <span className="text-muted">cpu {h.cpu?.toFixed(0) ?? "—"}%</span>
                      <span className="text-muted">mem {h.memory?.toFixed(0) ?? "—"}%</span>
                      <span className="text-muted">disk {h.disk?.toFixed(0) ?? "—"}%</span>
                    </div>
                  ) : null;
                })()}

                <div className="mt-3 flex items-center justify-between border-t border-border pt-2">
                  <span className="font-mono text-xs text-ink">{costText(s) || "no cost set"}</span>
                  <span className="flex items-center gap-2">
                    {s.paidUntil && <span className="font-mono text-xs text-faint">{s.paidUntil}</span>}
                    <DueBadge days={s.daysUntilDue} />
                  </span>
                </div>

                {s.lastDiscoveredAtUnixMs && (
                  <p className="mt-2 text-[10px] uppercase tracking-widest text-faint">
                    ⟳ auto-discovered {timeAgo(s.lastDiscoveredAtUnixMs)}
                  </p>
                )}
                {s.notes && <p className="mt-2 text-xs text-muted">{s.notes}</p>}
              </div>
            ))}
          </div>
        )}
      </section>

      <section>
        <SectionTitle>Topology — connections</SectionTitle>
        {links.length > 0 && (
          <div className="hud-panel mb-3 p-4">
            <TopologyGraph servers={servers} links={links} />
          </div>
        )}
        <div className="hud-panel p-4">
          {links.length === 0 ? (
            <p className="mb-3 text-sm text-muted">No connections yet. Link a proxy to the servers it fronts, or a DB to its app.</p>
          ) : (
            <ul className="mb-3 space-y-1.5">
              {links.map((l) => (
                <li key={l.id} className="flex items-center justify-between gap-2">
                  <span className="font-mono text-xs text-ink">
                    {nameById.get(l.fromServerId) ?? "?"}{" "}
                    <span className="text-accent">--[{l.kind}]--&gt;</span>{" "}
                    {nameById.get(l.toServerId) ?? "?"}
                  </span>
                  <button onClick={() => removeLink(l.id)} className="text-xs text-faint hover:text-danger" title="Remove link">✕</button>
                </li>
              ))}
            </ul>
          )}

          <form onSubmit={addLink} className="grid grid-cols-[1fr_auto_1fr_auto] items-center gap-2 text-sm">
            <select value={linkForm.fromServerId} onChange={(e) => setLinkForm({ ...linkForm, fromServerId: e.target.value })} className={inputClass}>
              <option value="">from…</option>
              {servers.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
            <span className="text-faint">→</span>
            <select value={linkForm.toServerId} onChange={(e) => setLinkForm({ ...linkForm, toServerId: e.target.value })} className={inputClass}>
              <option value="">to…</option>
              {servers.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
            <button type="submit" className="border border-accent px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent hover:bg-elevated" style={{ borderRadius: "var(--radius-md)" }}>
              Link
            </button>
            <input value={linkForm.kind} onChange={(e) => setLinkForm({ ...linkForm, kind: e.target.value })}
              placeholder="kind (proxy, database, depends-on)" className={`col-span-4 font-mono ${inputClass}`} />
          </form>
        </div>
      </section>
    </div>
  );
}
