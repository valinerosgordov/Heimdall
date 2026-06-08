import { ServerDto, ServerLinkDto } from "@/lib/api";

function statusColor(isUp: boolean | null): string {
  if (isUp === null) return "var(--text-muted, #8a8a9a)";
  return isUp ? "var(--success, #34d399)" : "var(--danger, #ff2d78)";
}

/**
 * Dependency-free SVG topology: servers on a circle, directed edges (proxy -> server, db, depends-on)
 * with arrowheads + kind labels. Node colour = linked health-check liveness.
 */
export default function TopologyGraph({ servers, links }: { servers: ServerDto[]; links: ServerLinkDto[] }) {
  if (servers.length < 2 || links.length === 0) return null;

  const W = 760;
  const H = Math.max(300, Math.min(520, 120 + servers.length * 44));
  const cx = W / 2;
  const cy = H / 2;
  const radius = Math.min(W, H) / 2 - 78;
  const n = servers.length;
  const nodeW = 134;
  const nodeH = 34;

  const pos = new Map<string, { x: number; y: number }>();
  servers.forEach((s, i) => {
    const angle = (i / n) * 2 * Math.PI - Math.PI / 2;
    pos.set(s.id, { x: cx + radius * Math.cos(angle), y: cy + radius * Math.sin(angle) });
  });

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="w-full" role="img" aria-label="Server topology graph" style={{ maxHeight: 520 }}>
      <defs>
        <marker id="topo-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
          <path d="M0,0 L10,5 L0,10 z" fill="var(--accent, #ff1e2d)" />
        </marker>
      </defs>

      {links.map((l) => {
        const a = pos.get(l.fromServerId);
        const b = pos.get(l.toServerId);
        if (!a || !b) return null;
        const dx = b.x - a.x;
        const dy = b.y - a.y;
        const len = Math.hypot(dx, dy) || 1;
        const ux = dx / len;
        const uy = dy / len;
        const x1 = a.x + ux * (nodeW / 2 - 4);
        const y1 = a.y + uy * (nodeH / 2);
        const x2 = b.x - ux * (nodeW / 2 - 4);
        const y2 = b.y - uy * (nodeH / 2 + 6);
        const mx = (x1 + x2) / 2;
        const my = (y1 + y2) / 2;
        return (
          <g key={l.id}>
            <line
              x1={x1}
              y1={y1}
              x2={x2}
              y2={y2}
              stroke="var(--accent, #ff1e2d)"
              strokeWidth={1.5}
              strokeOpacity={0.65}
              markerEnd="url(#topo-arrow)"
            />
            <text x={mx} y={my - 4} textAnchor="middle" fontFamily="monospace" fontSize={10} fill="var(--text-muted, #8a8a9a)">
              {l.kind}
            </text>
          </g>
        );
      })}

      {servers.map((s) => {
        const p = pos.get(s.id)!;
        const name = s.name.length > 15 ? `${s.name.slice(0, 14)}…` : s.name;
        return (
          <g key={s.id} transform={`translate(${p.x - nodeW / 2}, ${p.y - nodeH / 2})`}>
            <rect
              width={nodeW}
              height={nodeH}
              rx={6}
              fill="var(--color-elevated, #16161d)"
              stroke="var(--border-strong, rgba(255,255,255,0.16))"
            />
            <circle cx={13} cy={nodeH / 2} r={4} fill={statusColor(s.isUp)} />
            <text x={26} y={nodeH / 2 + 4} fontSize={12} fontWeight={600} fill="var(--color-ink, #e7e7f0)">
              {name}
            </text>
          </g>
        );
      })}
    </svg>
  );
}
