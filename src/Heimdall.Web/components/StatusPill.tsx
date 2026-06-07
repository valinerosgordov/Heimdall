export type StatusState = "up" | "warn" | "down" | "idle";

const STYLES: Record<StatusState, { color: string; glyph: string; label: string }> = {
  up: { color: "var(--success)", glyph: "●", label: "OK" },
  warn: { color: "var(--warning)", glyph: "▲", label: "WARN" },
  down: { color: "var(--danger)", glyph: "✕", label: "DOWN" },
  idle: { color: "var(--text-muted)", glyph: "○", label: "IDLE" },
};

/** Colour-blind-safe status atom: dot + glyph + uppercase label (never colour alone). */
export default function StatusPill({ state, label }: { state: StatusState; label?: string }) {
  const style = STYLES[state];
  return (
    <span
      className="inline-flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider"
      style={{ color: style.color }}
    >
      <span
        aria-hidden
        className="inline-block leading-none"
        style={{ animation: state === "down" ? "var(--animate-pulse-glow)" : undefined }}
      >
        {style.glyph}
      </span>
      {label ?? style.label}
    </span>
  );
}
