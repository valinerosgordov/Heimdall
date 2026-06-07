"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { login } from "@/lib/auth";

export default function LoginPage() {
  const router = useRouter();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    setBusy(true);
    const ok = await login(username, password);
    setBusy(false);
    if (ok) {
      router.replace("/");
    } else {
      setError("Invalid username or password.");
    }
  };

  return (
    <div className="flex min-h-[70vh] items-center justify-center">
      <form onSubmit={submit} className="hud-panel w-full max-w-sm p-6">
        <div className="mb-6 flex items-center gap-3">
          <span className="text-accent" aria-hidden>[//]</span>
          <span className="font-display text-2xl font-bold tracking-widest text-ink">HEIMDALL</span>
        </div>
        <label className="mb-1 block text-[10px] uppercase tracking-widest text-faint">Username</label>
        <input
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          autoFocus
          className="mb-4 w-full bg-elevated px-3 py-2 text-ink outline-none"
          style={{ borderRadius: "var(--radius-md)" }}
        />
        <label className="mb-1 block text-[10px] uppercase tracking-widest text-faint">Password</label>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="mb-5 w-full bg-elevated px-3 py-2 text-ink outline-none"
          style={{ borderRadius: "var(--radius-md)" }}
        />
        <button
          type="submit"
          disabled={busy}
          className="w-full border border-accent px-3 py-2 text-sm font-semibold uppercase tracking-wider text-accent hover:bg-elevated disabled:opacity-50"
          style={{ borderRadius: "var(--radius-md)" }}
        >
          {busy ? "Signing in…" : "Sign in"}
        </button>
        {error && <p className="mt-3 text-xs text-danger">{error}</p>}
      </form>
    </div>
  );
}
