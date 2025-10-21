import React from "react";
import {
  LayoutDashboard,
  FileText,
  Flag,
  XCircle,
  Bell,
  User,
  Settings,
  BarChart3,
  Tag
} from "lucide-react";
import "./ModeratorSidebar.css";

export default function ModeratorSidebar({ currentView, onViewChange }) {
  const menuItems = [
    {
      icon: <LayoutDashboard size={20} />,
      label: "All Posts",
      view: "overview",
      active: currentView === "overview"
    },
    {
      icon: <BarChart3 size={20} />,
      label: "Statistics",
      view: "statistics",
      active: currentView === "statistics"
    },
    {
      icon: <Tag size={20} />,
      label: "Tag Management",
      view: "tags",
      active: currentView === "tags"
    },
    {
      icon: <FileText size={20} />,
      label: "Pending Posts",
      view: "pending",
      active: currentView === "pending"
    },
    {
      icon: <Flag size={20} />,
      label: "Reported Posts",
      view: "reported",
      active: currentView === "reported"
    },
    {
      icon: <XCircle size={20} />,
      label: "Rejected Posts",
      view: "rejected",
      active: currentView === "rejected"
    },
  ];

  return (
    <aside className="moderator-sidebar">
      <div className="sidebar-header">
        <div className="logo">
          <User size={28} color="#007bff" />
          <span className="logo-text">Moderator</span>
        </div>
      </div>

      <nav className="sidebar-nav">
        {menuItems.map((item, index) => (
          <button
            key={index}
            className={`nav-item ${item.active ? "active" : ""}`}
            onClick={() => onViewChange(item.view)}
          >
            <span className="nav-icon">{item.icon}</span>
            <span className="nav-label">{item.label}</span>
          </button>
        ))}
      </nav>

      <div className="sidebar-footer">
        <div className="user-info">
          <User size={20} />
          <span>Moderator Panel</span>
        </div>
      </div>
    </aside>
  );
}


