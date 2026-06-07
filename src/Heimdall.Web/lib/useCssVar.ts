"use client";

import { useEffect, useState } from "react";

/** Reads a CSS custom property from :root so charts honour the active [data-palette]. */
export function useCssVar(name: string, fallback: string): string {
  const [value, setValue] = useState(fallback);
  useEffect(() => {
    const read = () => {
      const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
      if (v) setValue(v);
    };
    read();
    const observer = new MutationObserver(read);
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ["data-palette"] });
    return () => observer.disconnect();
  }, [name]);
  return value;
}
