// src/pages/Dashboard.tsx
import { useEffect, useState } from "react";
import api from "../api";
import { Line, Pie } from "react-chartjs-2";
import { Chart as ChartJS, ArcElement, CategoryScale, LinearScale, PointElement, LineElement, Tooltip, Legend } from "chart.js";
import dayjs from "dayjs";
import { useAuth } from "../context/AuthContext";
ChartJS.register(ArcElement, CategoryScale, LinearScale, PointElement, LineElement, Tooltip, Legend);

type Monthly = { Month: number; Income: number; Expense: number; };

export default function Dashboard() {
  const { userId } = useAuth();
  const [rows, setRows] = useState<Monthly[]>([]);

  useEffect(() => {
    const y = new Date().getFullYear();
    api.get("/api/transactions/summary/monthly", { params: { userId, year: y }})
       .then(r => setRows(r.data))
       .catch(()=> setRows([]));
  }, [userId]);

  const labels = rows.map(x => dayjs().month(x.Month - 1).format("MMM"));
  const income = rows.map(x => x.Income);
  const expense = rows.map(x => x.Expense);

  return (
    <div className="p-6">
      <h2 className="text-xl font-semibold mb-3">Spending Trends ({new Date().getFullYear()})</h2>
      <div className="grid md:grid-cols-2 gap-6">
        <div className="p-4 border rounded">
          <Line data={{ labels, datasets: [{ label: "Income", data: income }, { label: "Expense", data: expense }] }} />
        </div>
        <div className="p-4 border rounded">
          <h3 className="font-medium mb-2">This Month</h3>
          <Pie data={{ labels: ["Income","Expense"], datasets: [{ data: [income.at(-1) ?? 0, expense.at(-1) ?? 0] }] }} />
        </div>
      </div>
    </div>
  );
}
