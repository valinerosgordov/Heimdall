"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { getToken, logout } from "@/lib/auth";

const PALETTES = [
  { id: "crimson", label: "Crimson" },
  { id: "cyan", label: "Cyan" },
  { id: "copper", label: "Copper" },
];

function applyPalette(id: string) {
  if (id === "crimson") document.documentElement.removeAttribute("data-palette");
  else document.documentElement.setAttribute("data-palette", id);
}

export default function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const isLoginPage = pathname === "/login";

  const [clock, setClock] = useState("--:--:--");
  const [palette, setPalette] = useState("crimson");
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const saved = localStorage.getItem("heimdall-palette") ?? "crimson";
    setPalette(saved);
    applyPalette(saved);
  }, []);

  useEffect(() => {
    const id = setInterval(() => setClock(new Date().toLocaleTimeString("en-GB")), 1000);
    return () => clearInterval(id);
  }, []);

  useEffect(() => {
    if (isLoginPage) {
      setReady(true);
      return;
    }
    if (!getToken()) {
      setReady(false);
      router.replace("/login");
      return;
    }
    setReady(true);
  }, [pathname, isLoginPage, router]);

  const choosePalette = (id: string) => {
    setPalette(id);
    applyPalette(id);
    localStorage.setItem("heimdall-palette", id);
  };

  const doLogout = () => {
    logout();
    router.replace("/login");
  };

  // Login screen: bare canvas, no chrome, no guard.
  if (isLoginPage) return <main className="mx-auto max-w-6xl px-6 py-8">{children}</main>;

  // Avoid flashing protected content before the guard redirects.
  if (!ready) return null;

  return (
    <>
      <header className="sticky top-0 z-10 border-b border-border bg-base/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3 sm:px-6">
          <div className="flex items-center gap-3 sm:gap-6">
            <Link href="/" className="flex items-center gap-2">
              <span className="text-accent" aria-hidden>[//]</span>
              <span className="font-display text-xl font-bold tracking-widest text-ink">HEIMDALL</span>
            </Link>
            <nav className="flex gap-3 text-xs font-semibold uppercase tracking-wider sm:gap-4">
              <NavLink href="/" label="Overview" pathname={pathname} />
              <NavLink href="/infra" label="Infra" pathname={pathname} />
              <NavLink href="/alerts" label="Alerts" pathname={pathname} />
              <NavLink href="/settings" label="Settings" pathname={pathname} />
            </nav>
          </div>
          <div className="flex items-center gap-3 text-xs font-semibold uppercase tracking-wider text-muted sm:gap-5">
            <div className="hidden items-center gap-1 sm:flex">
              {PALETTES.map((p) => (
                <button
                  key={p.id}
                  onClick={() => choosePalette(p.id)}
                  className={`px-2 py-1 transition-colors ${palette === p.id ? "text-accent" : "text-faint hover:text-ink"}`}
                  style={{ borderRadius: "var(--radius-sm)" }}
                >
                  {p.label}
                </button>
              ))}
            </div>
            <span className="flex items-center gap-2">
              <span className="inline-block h-2 w-2 rounded-full bg-accent" style={{ animation: "var(--animate-pulse-glow)" }} />
              <span className="hidden sm:inline">LIVE</span>
            </span>
            <span className="hidden font-mono text-ink sm:inline">{clock}</span>
            <button onClick={doLogout} className="text-faint hover:text-danger">Logout</button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-6 py-8">{children}</main>
    </>
  );
}

function NavLink({ href, label, pathname }: { href: string; label: string; pathname: string }) {
  const active = href === "/" ? pathname === "/" : pathname.startsWith(href);
  return (
    <Link href={href} className={active ? "text-accent" : "text-faint hover:text-ink"}>
      {label}
    </Link>
  );
}
