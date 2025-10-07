// src/main.tsx
import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { AuthProvider, RequireAuth } from "./context/AuthContext";
import NavBar from "./components/NavBar";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Dashboard from "./pages/Dashboard";
import Categories from "./pages/Categories";
import Transactions from "./pages/Transactions";
import Budgets from "./pages/Budgets";
import "./index.css";

function AuthedLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <NavBar />
      <div>{children}</div>
    </>
  );
}

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login/>} />
          <Route path="/register" element={<Register/>} />
          <Route path="/" element={<RequireAuth><AuthedLayout><Dashboard/></AuthedLayout></RequireAuth>} />
          <Route path="/categories" element={<RequireAuth><AuthedLayout><Categories/></AuthedLayout></RequireAuth>} />
          <Route path="/transactions" element={<RequireAuth><AuthedLayout><Transactions/></AuthedLayout></RequireAuth>} />
          <Route path="/budgets" element={<RequireAuth><AuthedLayout><Budgets/></AuthedLayout></RequireAuth>} />
          <Route path="*" element={<div className="p-6">Not Found</div>} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  </React.StrictMode>
);
