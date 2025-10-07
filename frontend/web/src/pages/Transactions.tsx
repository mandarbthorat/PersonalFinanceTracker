// src/pages/Transactions.tsx
import { useEffect, useMemo, useState } from "react";
import api from "../api";
import dayjs from "dayjs";
import { useAuth } from "../context/AuthContext";

type TxType = "Income" | "Expense";
type Category = { id: string; name: string; isIncome: boolean };
type Tx = { id: string; amount: number; type: TxType; categoryId: string; occurredOn: string; note?: string };

export default function Transactions() {
  const { userId } = useAuth();
  const [cats, setCats] = useState<Category[]>([]);
  const [items, setItems] = useState<Tx[]>([]);
  const [amount, setAmount] = useState<number>(0);
  const [type, setType] = useState<TxType>("Expense");
  const [categoryId, setCategoryId] = useState<string>("");
  const [occurredOn, setOccurredOn] = useState<string>(dayjs().format("YYYY-MM-DD"));
  const [note, setNote] = useState("");

  const load = async () => {
    const [c, t] = await Promise.all([
      api.get("/api/categories", { params: { userId } }),
      api.get("/api/transactions", { params: { userId, from: dayjs().startOf("month").toISOString(), to: dayjs().endOf("month").toISOString() }})
    ]);
    setCats(c.data); setItems(t.data);
    if (!categoryId && c.data.length) setCategoryId(c.data[0].id);
  };

  useEffect(() => { load(); /* eslint-disable-next-line */ }, [userId]);

  async function add() {
    await api.post("/api/transactions", { userId, amount, type, categoryId, occurredOn, note });
    setAmount(0); setNote("");
    load();
  }

  const total = useMemo(() => ({
    inc: items.filter(i=>i.type==="Income").reduce((s,i)=>s+i.amount,0),
    exp: items.filter(i=>i.type==="Expense").reduce((s,i)=>s+i.amount,0),
  }), [items]);

  return (
    <div className="p-6">
      <h2 className="text-xl font-semibold mb-3">Transactions (This Month)</h2>

      <div className="p-4 border rounded mb-5 grid md:grid-cols-5 gap-3">
        <input className="border rounded px-3 py-2" type="number" step="0.01" placeholder="Amount" value={amount || ""} onChange={e=>setAmount(parseFloat(e.target.value || "0"))} />
        <select className="border rounded px-3 py-2" value={type} onChange={e=>setType(e.target.value as TxType)}>
          <option>Expense</option>
          <option>Income</option>
        </select>
        <select className="border rounded px-3 py-2" value={categoryId} onChange={e=>setCategoryId(e.target.value)}>
          {cats.filter(c=> (type==="Income") === c.isIncome).map(c=> <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        <input className="border rounded px-3 py-2" type="date" value={occurredOn} onChange={e=>setOccurredOn(e.target.value)} />
        <div className="md:col-span-5 flex gap-2">
          <input className="flex-1 border rounded px-3 py-2" placeholder="Note (optional)" value={note} onChange={e=>setNote(e.target.value)} />
          <button className="px-3 py-2 rounded bg-black text-white" onClick={add}>Add</button>
        </div>
      </div>

      <div className="flex gap-6 mb-4">
        <div className="px-3 py-2 border rounded bg-green-50">Income: ₹{total.inc.toFixed(2)}</div>
        <div className="px-3 py-2 border rounded bg-red-50">Expense: ₹{total.exp.toFixed(2)}</div>
      </div>

      <table className="w-full border">
        <thead className="bg-gray-50">
          <tr>
            <th className="p-2 text-left">Date</th>
            <th className="p-2 text-left">Type</th>
            <th className="p-2 text-left">Category</th>
            <th className="p-2 text-right">Amount</th>
            <th className="p-2 text-left">Note</th>
          </tr>
        </thead>
        <tbody>
          {items.map(t => (
            <tr key={t.id} className="border-t">
              <td className="p-2">{dayjs(t.occurredOn).format("DD MMM YYYY")}</td>
              <td className="p-2">{t.type}</td>
              <td className="p-2">{cats.find(c=>c.id===t.categoryId)?.name ?? "-"}</td>
              <td className="p-2 text-right">{t.amount.toFixed(2)}</td>
              <td className="p-2">{t.note ?? ""}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
