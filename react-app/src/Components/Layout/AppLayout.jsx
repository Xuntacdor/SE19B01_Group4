import React from "react";
import HeaderBar from "./HeaderBar";
import "./layout.css";

export default function AppLayout({ sidebar, title, children }) {
  return (
    <div className="app-shell">
      {sidebar && <div className="sidebar-container">{sidebar}</div>}

      <main className="app-main">
        <HeaderBar title={title} />
        {children}
      </main>
    </div>
  );
}
