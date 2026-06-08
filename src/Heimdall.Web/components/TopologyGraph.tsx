import { ServerDto, ServerLinkDto } from "@/lib/api";

function statusColor(isUp: boolean | null): string {
  if (isUp === null) return "var(--text-muted, #8a8a9a)";
  return isUp ? "var(--success, #34d399)" : "var(--danger, #ff2d78)";
}

/**
 * Layered (DAG) topology: sources on top, their targets below, arrows pointing down.
 * Only servers that participate in a link are drawn — unlinked servers stay in the cards/list.
 */
export default function TopologyGraph({ servers, links }: { servers: ServerDto[]; links: ServerLinkDto[] }) {
  const byId = new Map(servers.map((s) => [s.id, s]));
  const edges = links.filter((l) => byId.has(l.fromServerId) && byId.has(l.toServerId));
  if (edges.length === 0) return null;

  const inDeg = new Map<string, number>();
  const adj = new Map<string, string[]>();
  const touched = new Set<string>();
  for (const l of edges) {
    touched.add(l.fromServerId);
    touched.add(l.toServerId);
    inDeg.set(l.fromServerId, inDeg.get(l.fromServerId) ?? 0);
    inDeg.set(l.toServerId, (inDeg.get(l.toServerId) ?? 0) + 1);
    if (!adj.has(l.fromServerId)) adj.set(l.fromServerId, []);
    adj.get(l.fromServerId)!.push(l.toServerId);
  }
  const nodes = servers.filter((s) => touched.has(s.id));

  // Assign layers by BFS from the roots (no incoming edges).
  const layer = new Map<string, number>();
  let frontier = nodes.filter((s) => (inDeg.get(s.id) ?? 0) === 0).map((s) => s.id);
  if (frontier.length === 0) frontier = [nodes[0].id];
  const seen = new Set<string>();
  let depth = 0;
  while (frontier.length) {
    const next: string[] = [];
    for (const id of frontier) {
      if (seen.has(id)) continue;
      seen.add(id);
      layer.set(id, depth);
      for (const to of adj.get(id) ?? []) if (!seen.has(to)) next.push(to);
    }
    frontier = next;
    depth += 1;
  }
  nodes.forEach((s) => {
    if (!layer.has(s.id)) layer.set(s.id, 0);
  });

  const maxLayer = Math.max(0, ...layer.values());
  const rows: ServerDto[][] = [];
  for (let i = 0; i <= maxLayer; i += 1) rows.push(nodes.filter((s) => layer.get(s.id) === i));

  const NODE_W = 158;
  const NODE_H = 40;
  const ROW_GAP = 92;
  const COL_GAP = 44;
  const PAD = 28;
  const maxCols = Math.max(...rows.map((r) => r.length));
  const W = Math.max(560, maxCols * (NODE_W + COL_GAP) - COL_GAP + PAD * 2);
  const H = PAD * 2 + rows.length * NODE_H + (rows.length - 1) * ROW_GAP;

  const pos = new Map<string, { x: number; y: number }>();
  rows.forEach((row, ri) => {
    const rowW = row.length * NODE_W + (row.length - 1) * COL_GAP;
    const startX = (W - rowW) / 2;
    const y = PAD + ri * (NODE_H + ROW_GAP);
    row.forEach((s, ci) => pos.set(s.id, { x: startX + ci * (NODE_W + COL_GAP), y }));
  });

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="w-full" role="img" aria-label="Server topology" style={{ maxHeight: 520 }}>
      <defs>
        <marker id="topo-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
          <path d="M0,0 L10,5 L0,10 z" fill="var(--accent, #ff1e2d)" />
        </marker>
      </defs>

      {edges.map((l) => {
        const a = pos.get(l.fromServerId)!;
        const b = pos.get(l.toServerId)!;
        const x1 = a.x + NODE_W / 2;
        const y1 = a.y + NODE_H;
        const x2 = b.x + NODE_W / 2;
        const y2 = b.y;
        const my = (y1 + y2) / 2;
        return (
          <g key={l.id}>
            <path
              d={`M${x1},${y1} C${x1},${my} ${x2},${my} ${x2},${y2 - 6}`}
              fill="none"
              stroke="var(--accent, #ff1e2d)"
              strokeWidth={1.5}
              strokeOpacity={0.7}
              markerEnd="url(#topo-arrow)"
            />
            <text x={(x1 + x2) / 2} y={my - 4} textAnchor="middle" fontFamily="monospace" fontSize={10} fill="var(--text-muted, #8a8a9a)">
              {l.kind}
            </text>
          </g>
        );
      })}

      {nodes.map((s) => {
        const p = pos.get(s.id)!;
        const name = s.name.length > 17 ? `${s.name.slice(0, 16)}…` : s.name;
        return (
          <g key={s.id} transform={`translate(${p.x}, ${p.y})`}>
            <rect width={NODE_W} height={NODE_H} rx={7} fill="var(--color-elevated, #16161d)" stroke="var(--border-strong, rgba(255,255,255,0.16))" />
            <circle cx={14} cy={NODE_H / 2} r={4} fill={statusColor(s.isUp)} />
            <text x={28} y={NODE_H / 2 + 4} fontSize={12.5} fontWeight={600} fill="var(--color-ink, #e7e7f0)">
              {name}
            </text>
          </g>
        );
      })}
    </svg>
  );
}
