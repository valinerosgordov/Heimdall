import Link from "next/link";
import { OverviewHost } from "@/lib/api";
import StatusPill, { StatusState } from "./StatusPill";

function toneOf(value: number | null): StatusState {
  if (value === null) return "idle";
  if (value >= 90) return "down";
  if (value >= 70) return "warn";
  return "up";
}

function colorClass(value: number | null): string {
  if (value === null) return "text-faint";
  if (value >= 90) return "text-danger";
  if (value >= 70) return "text-warning";
  return "text-ink";
}

function Mini({ label, value }: { label: string; value: number | null }) {
  return (
    <div>
      <div className="text-[10px] uppercase tracking-widest text-faint">{label}</div>
      <div className={`font-mono text-lg tabular-nums ${colorClass(value)}`}>
        {value === null ? "--" : value.toFixed(0)}
        <span className="text-xs text-muted">%</span>
      </div>
    </div>
  );
}

export default function HostCard({ host }: { host: OverviewHost }) {
  const worst = Math.max(host.cpu ?? 0, host.memory ?? 0, host.disk ?? 0);
  const accentTone: StatusState = !host.online ? "down" : toneOf(worst);

  return (
    <Link href={`/host?name=${encodeURIComponent(host.name)}`} className="block">
      <div className="hud-panel p-4 transition-colors hover:bg-elevated" data-status={!host.online ? "down" : undefined}>
        <div className="flex items-center justify-between">
          <span className="font-heading text-base font-semibold text-ink">{host.name}</span>
          <StatusPill state={host.online ? "up" : "down"} label={host.online ? "ONLINE" : "OFFLINE"} />
        </div>
        <div className="mt-3 grid grid-cols-3 gap-2">
          <Mini label="CPU" value={host.cpu} />
          <Mini label="MEM" value={host.memory} />
          <Mini label="DISK" value={host.disk} />
        </div>
        <div
          className="mt-3 h-0.5 w-full"
          style={{ background: accentTone === "up" ? "var(--success)" : accentTone === "warn" ? "var(--warning)" : accentTone === "down" ? "var(--danger)" : "var(--border-subtle)" }}
        />
      </div>
    </Link>
  );
}
