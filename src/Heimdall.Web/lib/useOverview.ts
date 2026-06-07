"use client";

import { useEffect, useState } from "react";
import { apiBase, fetchOverview, OverviewSnapshot } from "./api";
import { getToken } from "./auth";

/** Live overview via Server-Sent Events, with a polling fallback if the stream drops. */
export function useOverview(): { data: OverviewSnapshot | null; live: boolean } {
  const [data, setData] = useState<OverviewSnapshot | null>(null);
  const [live, setLive] = useState(false);

  useEffect(() => {
    let stopped = false;
    let pollId: ReturnType<typeof setInterval> | null = null;

    const startPolling = () => {
      if (pollId) return;
      pollId = setInterval(async () => {
        try {
          const snapshot = await fetchOverview();
          if (!stopped) setData(snapshot);
        } catch {
          /* keep last good state */
        }
      }, 3000);
    };

    const stopPolling = () => {
      if (pollId) {
        clearInterval(pollId);
        pollId = null;
      }
    };

    // Seed immediately so the grid isn't empty before the first SSE frame.
    fetchOverview().then((d) => !stopped && setData(d)).catch(() => {});

    // EventSource can't set Authorization headers — pass the token via query string.
    const source = new EventSource(`${apiBase}/api/stream/overview?access_token=${encodeURIComponent(getToken() ?? "")}`);
    source.onmessage = (event) => {
      stopPolling(); // SSE is live again — drop the fallback poller to avoid duplicate load.
      try {
        setData(JSON.parse(event.data));
        setLive(true);
      } catch {
        /* ignore malformed frame */
      }
    };
    source.onerror = () => {
      setLive(false);
      startPolling();
    };

    return () => {
      stopped = true;
      source.close();
      if (pollId) clearInterval(pollId);
    };
  }, []);

  return { data, live };
}
