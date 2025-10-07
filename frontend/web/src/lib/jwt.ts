// src/lib/jwt.ts
export type JwtPayload = { sub?: string; email?: string; exp?: number; [k: string]: any };

function b64urlDecode(input: string): string {
  input = input.replace(/-/g, "+").replace(/_/g, "/");
  const pad = input.length % 4 ? 4 - (input.length % 4) : 0;
  const base64 = input + "=".repeat(pad);
  return decodeURIComponent(
    atob(base64)
      .split("")
      .map((c) => "%" + c.charCodeAt(0).toString(16).padStart(2, "0"))
      .join("")
  );
}

export function decodeJwt<T extends object = JwtPayload>(token: string): T | null {
  try {
    const [, payload] = token.split(".");
    return JSON.parse(b64urlDecode(payload));
  } catch {
    return null;
  }
}
