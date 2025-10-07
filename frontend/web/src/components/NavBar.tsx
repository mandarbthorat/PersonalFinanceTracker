// src/components/NavBar.tsx
import { Link, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export default function NavBar() {
  const { logout, email } = useAuth();
  const loc = useLocation();
  const link = (to: string, label: string) => (
    <Link className={`px-3 py-2 rounded ${loc.pathname === to ? "bg-gray-200" : ""}`} to={to}>{label}</Link>
  );
  return (
    <div className="w-full flex items-center justify-between px-4 py-3 border-b">
      <div className="flex items-center gap-3">
        <Link to="/" className="font-semibold">💸 Finance Tracker</Link>
        {link("/", "Dashboard")}
        {link("/transactions", "Transactions")}
        {link("/categories", "Categories")}
        {link("/budgets", "Budgets")}
      </div>
      <div className="flex items-center gap-3">
        <span className="text-sm text-gray-600">{email}</span>
        <button onClick={logout} className="px-3 py-1 rounded border">Logout</button>
      </div>
    </div>
  );
}
