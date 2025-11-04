import React from "react";
import HeaderBar from "./HeaderBar";
import "./layout.css";

export default function AppLayout({ sidebar, title, children }) {
  return (
    <div className="app-shell">
      {sidebar && <div className="sidebar-container">{sidebar}</div>}

      <main className={`app-main ${!sidebar ? "no-sidebar" : ""}`}>
        <HeaderBar title={title} />
        {children}
      </main>
    </div>
  );
}
