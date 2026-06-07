"use client";

import { useEffect, useRef } from "react";
import uPlot from "uplot";

/** Compact large axis values (30000 → "30k", 1.5e6 → "1.5M") so labels never clip. */
function formatCompact(value: number): string {
  const abs = Math.abs(value);
  if (abs >= 1e9) return `${(value / 1e9).toFixed(1)}G`;
  if (abs >= 1e6) return `${(value / 1e6).toFixed(1)}M`;
  if (abs >= 1e3) return `${(value / 1e3).toFixed(0)}k`;
  return String(value);
}

export interface UplotSeriesDef {
  label: string;
  color: string;
}

interface Props {
  data: uPlot.AlignedData;
  seriesDefs: UplotSeriesDef[];
  height?: number;
  /** "percent" pins the Y axis to 0–100; "auto" lets uPlot scale (e.g. network bytes). */
  yMode?: "percent" | "auto";
}

export default function UplotChart({ data, seriesDefs, height = 240, yMode = "percent" }: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const plotRef = useRef<uPlot | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const options: uPlot.Options = {
      width: container.clientWidth,
      height,
      padding: [12, 16, 4, 4],
      cursor: { y: false, points: { size: 6 } },
      legend: { show: false },
      scales: {
        x: { time: true },
        y: yMode === "percent"
          ? { range: (_u, _min, max) => [0, Math.max(100, max ?? 100)] }
          : {},
      },
      axes: [
        {
          stroke: "#5A5A5A",
          grid: { stroke: "rgba(255,255,255,0.06)", width: 1 },
          ticks: { stroke: "rgba(255,255,255,0.06)", width: 1 },
        },
        {
          stroke: "#5A5A5A",
          // Percent labels fit in 44px; auto-scaled (e.g. network bytes) need more room + compact formatting.
          size: yMode === "percent" ? 44 : 60,
          grid: { stroke: "rgba(255,255,255,0.06)", width: 1 },
          ticks: { stroke: "rgba(255,255,255,0.06)", width: 1 },
          ...(yMode === "auto"
            ? { values: (_u: uPlot, splits: number[]) => splits.map(formatCompact) }
            : {}),
        },
      ],
      series: [
        {},
        ...seriesDefs.map((s) => ({
          label: s.label,
          stroke: s.color,
          width: 2,
          points: { show: false },
        })),
      ],
    };

    const plot = new uPlot(options, data, container);
    plotRef.current = plot;

    const onResize = () => plot.setSize({ width: container.clientWidth, height });
    window.addEventListener("resize", onResize);

    return () => {
      window.removeEventListener("resize", onResize);
      plot.destroy();
      plotRef.current = null;
    };
    // Rebuild only when the series definitions change; data updates flow through setData below.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [height, yMode, JSON.stringify(seriesDefs)]);

  useEffect(() => {
    plotRef.current?.setData(data);
  }, [data]);

  return <div ref={containerRef} className="w-full" />;
}
