// src/pages/Categories.tsx
import { useEffect, useState } from "react";
import api from "../api";
import { useAuth } from "../context/AuthContext";

type Category = { id: string; name: string; isIncome: boolean };

export default function Categories() {
  const { userId } = useAuth();
  const [items, setItems] = useState<Category[]>([]);
  const [name, setName] = useState("");
  const [isIncome, setIsIncome] = useState(false);

  const load = () => api.get("/api/categories", { params: { userId } }).then(r => setItems(r.data));

  useEffect(() => { load(); }, [userId]);

  async function add() {
    await api.post("/api/categories", { userId, name, isIncome });
    setName(""); setIsIncome(false);
    load();
  }

  return (
    <div className="p-6">
      <h2 className="text-xl font-semibold mb-3">Categories</h2>
      <div className="flex gap-2 mb-4">
        <input className="border rounded px-3 py-2" placeholder="Name" value={name} onChange={e=>setName(e.target.value)} />
        <label className="flex items-center gap-2">
          <input type="checkbox" checked={isIncome} onChange={e=>setIsIncome(e.target.checked)} /> Income?
        </label>
        <button className="px-3 py-2 rounded bg-black text-white" onClick={add}>Add</button>
      </div>
      <ul className="space-y-2">
        {items.map(c => (
          <li key={c.id} className="p-3 border rounded flex items-center justify-between">
            <span>{c.name}</span>
            <span className={`text-xs px-2 py-1 rounded ${c.isIncome ? "bg-green-100 text-green-700" : "bg-red-100 text-red-700"}`}>
              {c.isIncome ? "Income" : "Expense"}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}
