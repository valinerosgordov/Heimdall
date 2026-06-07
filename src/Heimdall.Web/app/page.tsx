"use client";

import AddHealthCheck from "@/components/AddHealthCheck";
import HealthBoard from "@/components/HealthBoard";
import HostCard from "@/components/HostCard";
import { deleteHealthCheck } from "@/lib/api";
import { useOverview } from "@/lib/useOverview";

function SectionTitle({ children }: { children: React.ReactNode }) {
  return <h2 className="font-heading text-sm font-semibold uppercase tracking-[0.14em] text-faint">{children}</h2>;
}

export default function OverviewPage() {
  const { data } = useOverview();
  const hosts = data?.hosts ?? [];
  const checks = data?.healthChecks ?? [];
  const online = hosts.filter((h) => h.online).length;

  return (
    <div className="space-y-10">
      <section>
        <div className="mb-3 flex items-baseline justify-between">
          <SectionTitle>Hosts</SectionTitle>
          {hosts.length > 0 && (
            <span className="font-mono text-xs tabular-nums text-faint">{online}/{hosts.length} online</span>
          )}
        </div>
        {hosts.length === 0 ? (
          <p className="text-sm text-muted">No hosts reporting yet. Enroll the agent and tiles will appear within a few seconds.</p>
        ) : (
          <div className="grid gap-4 [grid-template-columns:repeat(auto-fill,minmax(240px,1fr))]">
            {hosts.map((host) => (
              <HostCard key={host.id} host={host} />
            ))}
          </div>
        )}
      </section>

      <section>
        <div className="mb-3 flex items-baseline justify-between">
          <SectionTitle>Health Checks</SectionTitle>
          <span className="text-xs text-faint">HTTP / TCP up · down · latency</span>
        </div>
        <AddHealthCheck />
        <HealthBoard checks={checks} onDelete={(id) => void deleteHealthCheck(id)} />
      </section>
    </div>
  );
}
