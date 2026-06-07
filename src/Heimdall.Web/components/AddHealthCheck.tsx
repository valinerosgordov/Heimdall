"use client";

import { useState } from "react";
import { CreateHealthCheckInput, createHealthCheck } from "@/lib/api";

const EMPTY: CreateHealthCheckInput = { name: "", kind: "Http", target: "", intervalSeconds: 30 };

export default function AddHealthCheck() {
  const [form, setForm] = useState<CreateHealthCheckInput>(EMPTY);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    setBusy(true);
    const ok = await createHealthCheck(form);
    setBusy(false);
    if (!ok) {
      setError("Could not add — check the values (HTTP: an http(s) URL; TCP: host:port).");
      return;
    }
    setForm(EMPTY);
  };

  return (
    <form onSubmit={submit} className="hud-panel mb-3 grid grid-cols-2 gap-2 p-4 text-sm md:grid-cols-[1fr_auto_2fr_auto_auto]">
      <input
        required
        placeholder="Name"
        value={form.name}
        onChange={(e) => setForm({ ...form, name: e.target.value })}
        className="col-span-2 bg-elevated px-2 py-1.5 text-ink outline-none md:col-span-1"
      />
      <select
        value={form.kind}
        onChange={(e) => setForm({ ...form, kind: e.target.value })}
        className="bg-elevated px-2 py-1.5 text-ink outline-none"
      >
        <option value="Http">HTTP</option>
        <option value="Tcp">TCP</option>
      </select>
      <input
        required
        placeholder={form.kind === "Tcp" ? "host:port" : "https://host/health"}
        value={form.target}
        onChange={(e) => setForm({ ...form, target: e.target.value })}
        className="col-span-2 bg-elevated px-2 py-1.5 font-mono text-ink outline-none md:col-span-1"
      />
      <input
        type="number"
        title="Probe interval (seconds)"
        value={form.intervalSeconds}
        onChange={(e) => setForm({ ...form, intervalSeconds: Number(e.target.value) })}
        className="bg-elevated px-2 py-1.5 font-mono text-ink outline-none"
      />
      <button
        type="submit"
        disabled={busy}
        className="border border-accent px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent hover:bg-elevated disabled:opacity-50"
        style={{ borderRadius: "var(--radius-md)" }}
      >
        {busy ? "Adding…" : "Add monitor"}
      </button>
      {error && <p className="col-span-2 text-xs text-danger md:col-span-5">{error}</p>}
    </form>
  );
}
