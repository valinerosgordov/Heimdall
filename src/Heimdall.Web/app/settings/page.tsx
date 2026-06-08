"use client";

import { useEffect, useState } from "react";
import StatusPill from "@/components/StatusPill";
import { ChannelStatus, fetchChannelStatus, sendTestNotification } from "@/lib/api";

function SectionTitle({ children }: { children: React.ReactNode }) {
  return <h2 className="mb-3 font-heading text-sm font-semibold uppercase tracking-[0.14em] text-faint">{children}</h2>;
}

export default function SettingsPage() {
  const [channels, setChannels] = useState<ChannelStatus[]>([]);
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<string | null>(null);

  const refresh = async () => {
    try {
      setChannels(await fetchChannelStatus());
    } catch {
      /* keep last good state */
    }
  };

  useEffect(() => {
    void refresh();
  }, []);

  const anyConfigured = channels.some((c) => c.configured);

  const test = async () => {
    setBusy(true);
    setResult(null);
    const r = await sendTestNotification();
    setBusy(false);
    if (!r) setResult("Test failed.");
    else if (r.channelsNotified === 0) setResult("No channels configured.");
    else setResult(`Sent to ${r.channelsNotified} channel(s): ${r.channels.join(", ")}.`);
  };

  return (
    <div className="space-y-10">
      <section>
        <SectionTitle>Notifications</SectionTitle>
        <div className="hud-panel p-4">
          {channels.length === 0 ? (
            <p className="text-sm text-muted">No channels found.</p>
          ) : (
            <div className="mb-4 space-y-2">
              {channels.map((c) => (
                <div key={c.name} className="flex items-center justify-between border-b border-border pb-2 last:border-b-0">
                  <span className="font-heading text-sm text-ink">{c.name}</span>
                  <StatusPill state={c.configured ? "up" : "idle"} label={c.configured ? "CONFIGURED" : "OFF"} />
                </div>
              ))}
            </div>
          )}

          <button
            onClick={test}
            disabled={busy || !anyConfigured}
            className="border border-accent px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent hover:bg-elevated disabled:opacity-40"
            style={{ borderRadius: "var(--radius-md)" }}
          >
            {busy ? "Sending…" : "Send test"}
          </button>
          {result && <p className="mt-3 text-xs text-muted">{result}</p>}

          <p className="mt-4 text-xs text-faint">
            Configure channels in <span className="font-mono text-muted">appsettings.Development.json</span>:{" "}
            <span className="font-mono text-muted">Heimdall:Telegram</span> (BotToken + ChatId) and/or{" "}
            <span className="font-mono text-muted">Heimdall:Email</span> (Host / From / To). Restart to apply — then they carry
            alerts and renewal reminders.
          </p>
        </div>
      </section>
    </div>
  );
}
