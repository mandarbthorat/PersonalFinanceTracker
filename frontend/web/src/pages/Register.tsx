// src/pages/Register.tsx
import type { FormEvent } from "react";
import { useState } from "react";
import api from "../api";

export default function Register() {
  const [email, setEmail] = useState(""); 
  const [password, setPassword] = useState("");
  const [ok, setOk] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setErr(null);
    try {
      // JSON body (recommended). If your API expects query, use { params: { email, password } }.
      await api.post("/api/auth/register", { email, password });
      setOk(true);
    } catch (ex: any) {
      setErr(ex?.response?.data ?? "Registration failed");
    } finally { setBusy(false); }
  }

  if (ok) return (
    <div className="max-w-sm mx-auto mt-20 p-6 border rounded">
      <h2 className="text-xl font-semibold mb-2">Account created</h2>
      <p className="mb-3">Your account has been created. You can now sign in.</p>
      <a href="/login" className="underline">Go to Sign in</a>
    </div>
  );

  return (
    <div className="max-w-sm mx-auto mt-20 p-6 border rounded">
      <h2 className="text-xl font-semibold mb-4">Create account</h2>
      <form onSubmit={onSubmit} className="flex flex-col gap-3">
        <input className="border rounded px-3 py-2" type="email" placeholder="Email" value={email} onChange={e=>setEmail(e.target.value)} required />
        <input className="border rounded px-3 py-2" type="password" placeholder="Password" value={password} onChange={e=>setPassword(e.target.value)} required />
        {err && <div className="text-red-600 text-sm">{err}</div>}
        <button disabled={busy} className="px-3 py-2 rounded bg-black text-white">{busy ? "Creating..." : "Create account"}</button>
      </form>
    </div>
  );
}
