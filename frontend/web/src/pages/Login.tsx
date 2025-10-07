// src/pages/Login.tsx
import type { FormEvent } from "react";
import { useState } from "react";
import api from "../api";
import { useAuth } from "../context/AuthContext";

export default function Login() {
  const { login } = useAuth();
  const [email, setEmail] = useState(""); 
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setError(null);
    try {
      // JSON body version (recommended). If your API expects query params, replace with: api.post("/api/auth/login", null, { params: { email, password } })
      const { data } = await api.post("/api/auth/login", { email, password });
      if (!data?.token) throw new Error("No token returned");
      login(data.token);
      window.location.href = "/";
    } catch (err: any) {
      setError(err?.response?.data ?? "Login failed");
    } finally { setBusy(false); }
  }

  return (
    <div className="max-w-sm mx-auto mt-20 p-6 border rounded">
      <h2 className="text-xl font-semibold mb-4">Sign in</h2>
      <form onSubmit={onSubmit} className="flex flex-col gap-3">
        <input className="border rounded px-3 py-2" type="email" placeholder="Email" value={email} onChange={e=>setEmail(e.target.value)} required />
        <input className="border rounded px-3 py-2" type="password" placeholder="Password" value={password} onChange={e=>setPassword(e.target.value)} required />
        {error && <div className="text-red-600 text-sm">{error}</div>}
        <button disabled={busy} className="px-3 py-2 rounded bg-black text-white">{busy ? "Signing in..." : "Sign in"}</button>
      </form>
      <div className="text-sm mt-3">No account? <a className="underline" href="/register">Create one</a></div>
    </div>
  );
}
