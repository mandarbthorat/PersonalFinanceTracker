// src/context/AuthContext.tsx
import { createContext, useContext, useEffect, useMemo, useState,  } from "react";
import { decodeJwt } from "../lib/jwt";
import { setAuthToken } from "../api";

type AuthState = { token?: string; userId?: string; email?: string };
type AuthCtx = AuthState & {
  login: (token: string) => void;
  logout: () => void;
};
const Ctx = createContext<AuthCtx | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | undefined>(() => localStorage.getItem("token") || undefined);
  const [userId, setUserId] = useState<string | undefined>(() => localStorage.getItem("userId") || undefined);
  const [email, setEmail] = useState<string | undefined>(() => localStorage.getItem("email") || undefined);

  useEffect(() => setAuthToken(token), [token]);

  const login = (t: string) => {
    const payload = decodeJwt(t) || {};
    const uid = (payload as any).sub as string | undefined;
    const em = (payload as any).email as string | undefined;
    setToken(t); setUserId(uid); setEmail(em);
    localStorage.setItem("token", t);
    if (uid) localStorage.setItem("userId", uid);
    if (em) localStorage.setItem("email", em);
  };
  const logout = () => {
    setToken(undefined); setUserId(undefined); setEmail(undefined);
    localStorage.removeItem("token"); localStorage.removeItem("userId"); localStorage.removeItem("email");
    setAuthToken(undefined);
  };

  const value = useMemo(() => ({ token, userId, email, login, logout }), [token, userId, email]);
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}
    
export const useAuth = () => {
  const v = useContext(Ctx);
  if (!v) throw new Error("useAuth must be used within AuthProvider");
  return v;
};

export function RequireAuth({ children }: { children: React.ReactElement }) {
  const { token } = useAuth();
  if (!token) {
    // basic redirect
    window.location.href = "/login";
    return null;
  }
  return children;
}
