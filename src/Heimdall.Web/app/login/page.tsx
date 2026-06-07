"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { authStatus, login, setupOperator } from "@/lib/auth";

type Mode = "loading" | "login" | "setup";

export default function LoginPage() {
  const router = useRouter();
  const [mode, setMode] = useState<Mode>("loading");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    void authStatus().then((configured) => setMode(configured ? "login" : "setup"));
  }, []);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);

    if (mode === "setup") {
      if (password.length < 8) {
        setError("Password must be at least 8 characters.");
        return;
      }
      if (password !== confirm) {
        setError("Passwords do not match.");
        return;
      }
      setBusy(true);
      const ok = await setupOperator(username.trim(), password);
      setBusy(false);
      if (ok) router.replace("/");
      else setError("Could not create the account — it may already exist.");
      return;
    }

    setBusy(true);
    const ok = await login(username, password);
    setBusy(false);
    if (ok) router.replace("/");
    else setError("Invalid username or password.");
  };

  if (mode === "loading") return null;
  const isSetup = mode === "setup";
  const inputClass = "mb-4 w-full bg-elevated px-3 py-2 text-ink outline-none";

  return (
    <div className="flex min-h-[70vh] items-center justify-center">
      <form onSubmit={submit} className="hud-panel w-full max-w-sm p-6">
        <div className="mb-5 flex items-center gap-3">
          <span className="text-accent" aria-hidden>[//]</span>
          <span className="font-display text-2xl font-bold tracking-widest text-ink">HEIMDALL</span>
        </div>
        <p className="mb-5 text-xs uppercase tracking-widest text-faint">
          {isSetup ? "First run — create your admin account" : "Sign in"}
        </p>

        <label className="mb-1 block text-[10px] uppercase tracking-widest text-faint">Username</label>
        <input
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          autoFocus
          className={inputClass}
          style={{ borderRadius: "var(--radius-md)" }}
        />

        <label className="mb-1 block text-[10px] uppercase tracking-widest text-faint">Password</label>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className={isSetup ? inputClass : "mb-5 w-full bg-elevated px-3 py-2 text-ink outline-none"}
          style={{ borderRadius: "var(--radius-md)" }}
        />

        {isSetup && (
          <>
            <label className="mb-1 block text-[10px] uppercase tracking-widest text-faint">Confirm password</label>
            <input
              type="password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              className="mb-5 w-full bg-elevated px-3 py-2 text-ink outline-none"
              style={{ borderRadius: "var(--radius-md)" }}
            />
          </>
        )}

        <button
          type="submit"
          disabled={busy}
          className="w-full border border-accent px-3 py-2 text-sm font-semibold uppercase tracking-wider text-accent hover:bg-elevated disabled:opacity-50"
          style={{ borderRadius: "var(--radius-md)" }}
        >
          {busy ? (isSetup ? "Creating…" : "Signing in…") : isSetup ? "Create account" : "Sign in"}
        </button>
        {error && <p className="mt-3 text-xs text-danger">{error}</p>}
      </form>
    </div>
  );
}
