"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useMemo, useState } from "react";
import RadialGauge from "@/components/RadialGauge";
import UplotChart from "@/components/UplotChart";
import { fetchHostMetrics, HostMetrics } from "@/lib/api";
import { useCssVar } from "@/lib/useCssVar";

const RANGES = [
  { label: "15m", minutes: 15 },
  { label: "1h", minutes: 60 },
  { label: "6h", minutes: 360 },
  { label: "24h", minutes: 1440 },
];

const ALL_METRICS = ["cpu.usage", "memory.usage", "disk.usage", "net.rx_bytes_per_sec", "net.tx_bytes_per_sec"];

type Aligned = [number[], ...(number | null)[][]];

function buildAligned(metrics: HostMetrics | null, keys: string[]): Aligned {
  if (!metrics) return [[], ...keys.map(() => [])] as Aligned;

  const timestamps = new Set<number>();
  for (const key of keys) {
    metrics.series.find((s) => s.metric === key)?.points.forEach((p) => timestamps.add(p.timestampUnixMs));
  }
  const sorted = [...timestamps].sort((a, b) => a - b);
  const xs = sorted.map((t) => t / 1000);
  const columns = keys.map((key) => {
    const map = new Map(metrics.series.find((s) => s.metric === key)?.points.map((p) => [p.timestampUnixMs, p.value]) ?? []);
    return sorted.map((t) => map.get(t) ?? null);
  });
  return [xs, ...columns] as Aligned;
}

function latestOf(metrics: HostMetrics | null, key: string): number | null {
  return metrics?.series.find((s) => s.metric === key)?.points.at(-1)?.value ?? null;
}

export default function HostDetailPage() {
  return (
    <Suspense fallback={<p className="text-sm text-muted">Loading…</p>}>
      <HostDetail />
    </Suspense>
  );
}

function HostDetail() {
  const name = useSearchParams().get("name") ?? "";
  const [minutes, setMinutes] = useState(15);
  const [metrics, setMetrics] = useState<HostMetrics | null>(null);

  const accent = useCssVar("--accent", "#FF1E2D");
  const cyan = useCssVar("--series-2", "#4DD0E1");
  const purple = useCssVar("--series-4", "#A78BFA");

  useEffect(() => {
    if (!name) return;
    let stopped = false;
    const tick = async () => {
      try {
        const result = await fetchHostMetrics(name, minutes, 500, ALL_METRICS);
        if (!stopped) setMetrics(result);
      } catch {
        /* transient */
      }
    };
    void tick();
    const id = setInterval(tick, minutes <= 60 ? 2000 : 15000);
    return () => {
      stopped = true;
      clearInterval(id);
    };
  }, [name, minutes]);

  const cpuMem = useMemo(() => buildAligned(metrics, ["cpu.usage", "memory.usage"]), [metrics]);
  const net = useMemo(() => buildAligned(metrics, ["net.rx_bytes_per_sec", "net.tx_bytes_per_sec"]), [metrics]);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <Link href="/" className="text-xs uppercase tracking-widest text-faint hover:text-ink">&lt; Hosts</Link>
          <h1 className="font-display text-2xl font-bold text-ink">{name || "—"}</h1>
        </div>
        <div className="flex gap-1">
          {RANGES.map((range) => (
            <button
              key={range.minutes}
              onClick={() => setMinutes(range.minutes)}
              className={`px-3 py-1 text-xs font-semibold uppercase tracking-wider transition-colors ${
                minutes === range.minutes ? "border border-accent text-accent" : "border border-border text-muted hover:text-ink"
              }`}
              style={{ borderRadius: "var(--radius-md)" }}
            >
              {range.label}
            </button>
          ))}
        </div>
      </div>

      <div className="hud-panel flex flex-wrap justify-around gap-6 p-6">
        <RadialGauge label="CPU" value={latestOf(metrics, "cpu.usage")} />
        <RadialGauge label="Memory" value={latestOf(metrics, "memory.usage")} />
        <RadialGauge label="Disk" value={latestOf(metrics, "disk.usage")} />
      </div>

      <Panel title="CPU & Memory %">
        <UplotChart data={cpuMem as never} seriesDefs={[{ label: "CPU %", color: accent }, { label: "MEM %", color: cyan }]} />
      </Panel>

      <Panel title="Network — bytes/sec">
        <UplotChart data={net as never} seriesDefs={[{ label: "RX", color: cyan }, { label: "TX", color: purple }]} yMode="auto" />
      </Panel>
    </div>
  );
}

function Panel({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="hud-panel p-4">
      <div className="mb-3 border-b border-border pb-2">
        <h2 className="font-heading text-lg font-semibold uppercase tracking-[0.06em] text-ink">{title}</h2>
      </div>
      {children}
    </div>
  );
}
