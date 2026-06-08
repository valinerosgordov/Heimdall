import { HealthCheckStatus } from "@/lib/api";
import StatusPill, { StatusState } from "./StatusPill";

function stateOf(check: HealthCheckStatus): StatusState {
  if (check.isUp === null) return "idle";
  return check.isUp ? "up" : "down";
}

// For TLS checks the "latency" value carries days-to-certificate-expiry.
function certColor(days: number): string {
  if (days < 7) return "var(--danger)";
  if (days < 30) return "var(--warning)";
  return "var(--success)";
}

export default function HealthBoard({
  checks,
  onDelete,
}: {
  checks: HealthCheckStatus[];
  onDelete?: (id: string) => void;
}) {
  if (checks.length === 0) {
    return <p className="text-sm text-muted">No monitors yet — add one above.</p>;
  }

  return (
    <div className="hud-panel overflow-hidden">
      <div className="grid grid-cols-[1fr_auto_auto_auto_auto] gap-4 border-b border-border px-4 py-2 text-[10px] uppercase tracking-widest text-faint">
        <span>Target</span>
        <span>Latency</span>
        <span>24h</span>
        <span>Status</span>
        <span></span>
      </div>
      {checks.map((check) => (
        <div
          key={check.id}
          className="grid grid-cols-[1fr_auto_auto_auto_auto] items-center gap-4 border-b border-border px-4 py-3 last:border-b-0"
          data-status={check.isUp === false ? "down" : undefined}
        >
          <div className="min-w-0">
            <div className="truncate font-heading text-sm font-semibold text-ink">{check.name}</div>
            <div className="truncate font-mono text-xs text-muted">
              <span className="text-faint">{check.kind.toUpperCase()}</span> {check.target}
            </div>
          </div>
          <div className="font-mono text-sm tabular-nums text-muted">
            {check.latencyMs === null ? (
              "--"
            ) : check.kind.toLowerCase() === "tls" ? (
              <span style={{ color: certColor(check.latencyMs) }} title="Days until the certificate expires">
                {check.latencyMs.toFixed(0)}d
              </span>
            ) : (
              `${check.latencyMs.toFixed(0)} ms`
            )}
          </div>
          <div
            className="font-mono text-xs tabular-nums"
            title="Uptime over the last 24h"
            style={{
              color:
                check.uptime24h === null
                  ? "var(--text-muted)"
                  : check.uptime24h >= 99
                    ? "var(--success)"
                    : check.uptime24h >= 95
                      ? "var(--warning)"
                      : "var(--danger)",
            }}
          >
            {check.uptime24h === null ? "—" : `${check.uptime24h.toFixed(1)}%`}
          </div>
          <StatusPill state={stateOf(check)} />
          {onDelete ? (
            <button
              onClick={() => onDelete(check.id)}
              className="text-xs uppercase tracking-wider text-faint hover:text-danger"
              title="Remove monitor"
            >
              ✕
            </button>
          ) : (
            <span />
          )}
        </div>
      ))}
    </div>
  );
}
