const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5087";
const TOKEN_KEY = "heimdall-token";

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
}

export async function login(username: string, password: string): Promise<boolean> {
  const response = await fetch(`${API_BASE}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password }),
  });
  if (!response.ok) return false;
  const data = (await response.json()) as { accessToken: string };
  setToken(data.accessToken);
  return true;
}

export function logout(): void {
  clearToken();
}
