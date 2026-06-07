function zoneColor(value: number): string {
  if (value >= 90) return "var(--danger)";
  if (value >= 70) return "var(--warning)";
  return "var(--success)";
}

/** A 0–100% ring gauge whose fill colour shifts by threshold. */
export default function RadialGauge({ label, value, unit = "%" }: { label: string; value: number | null; unit?: string }) {
  const pct = value === null ? 0 : Math.max(0, Math.min(100, value));
  const color = zoneColor(pct);

  return (
    <div className="flex flex-col items-center gap-2">
      <div
        className="relative h-28 w-28 rounded-full"
        style={{ background: `conic-gradient(${color} ${pct * 3.6}deg, rgba(255,255,255,0.06) 0)` }}
      >
        <div className="absolute inset-[10px] flex flex-col items-center justify-center rounded-full bg-panel">
          <span className="font-mono text-2xl font-semibold tabular-nums text-ink">
            {value === null ? "--" : pct.toFixed(0)}
          </span>
          <span className="text-[10px] uppercase tracking-widest text-faint">{unit}</span>
        </div>
      </div>
      <span className="text-xs uppercase tracking-widest text-muted">{label}</span>
    </div>
  );
}
