// src/pages/Budgets.tsx
import { useEffect, useState } from "react";
import api from "../api";
import { useAuth } from "../context/AuthContext";

type BudgetRow = { id: string; categoryId: string; categoryName: string; amount: number; spent: number; month: number; year: number };

export default function Budgets() {
  const { userId } = useAuth();
  const [rows, setRows] = useState<BudgetRow[]>([]);
  const [categoryId, setCategoryId] = useState<string>("");
  const [amount, setAmount] = useState<number>(0);
  const [cats, setCats] = useState<{ id: string; name: string; isIncome: boolean }[]>([]);

  const load = async () => {
    const [b, c] = await Promise.all([
      api.get("/api/budgets/current", { params: { userId } }),
      api.get("/api/categories", { params: { userId } }),
    ]);
    setRows(b.data); setCats(c.data.filter((x:any)=>!x.isIncome));
    if (!categoryId && c.data.length) setCategoryId(c.data[0].id);
  };

  useEffect(() => { load(); /* eslint-disable-next-line */ }, [userId]);

  async function add() {
    const now = new Date();
    await api.post("/api/budgets", { userId, categoryId, amount, month: now.getMonth()+1, year: now.getFullYear() });
    setAmount(0);
    load();
  }

  return (
    <div className="p-6">
      <h2 className="text-xl font-semibold mb-3">Budgets (Current Month)</h2>

      <div className="flex gap-2 mb-4">
        <select className="border rounded px-3 py-2" value={categoryId} onChange={e=>setCategoryId(e.target.value)}>
          {cats.map(c=> <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        <input className="border rounded px-3 py-2" type="number" placeholder="Amount" value={amount || ""} onChange={e=>setAmount(parseFloat(e.target.value || "0"))} />
        <button className="px-3 py-2 rounded bg-black text-white" onClick={add}>Set Budget</button>
      </div>

      <table className="w-full border">
        <thead className="bg-gray-50">
          <tr>
            <th className="p-2 text-left">Category</th>
            <th className="p-2 text-right">Budget</th>
            <th className="p-2 text-right">Spent</th>
            <th className="p-2 text-right">Remaining</th>
          </tr>
        </thead>
        <tbody>
          {rows.map(r => {
            const remaining = (r.amount ?? 0) - (r.spent ?? 0);
            return (
              <tr key={r.id} className="border-t">
                <td className="p-2">{r.categoryName}</td>
                <td className="p-2 text-right">₹{r.amount?.toFixed(2)}</td>
                <td className="p-2 text-right">₹{r.spent?.toFixed(2)}</td>
                <td className={`p-2 text-right ${remaining < 0 ? "text-red-600" : ""}`}>₹{remaining.toFixed(2)}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
