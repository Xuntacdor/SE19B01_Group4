import React from "react";
import { GraduationCap, Cloud, Wallet } from "lucide-react";
import styles from "./ModeratorNavbar.module.css";
import {
  LayoutDashboard,
  BarChart3,
  Tag,
  FileText,
  Flag,
  XCircle,
} from "lucide-react";

export default function ModeratorNavbar({ currentView, onViewChange }) {
  const menuItems = [
    {
      icon: <LayoutDashboard size={20} />,
      label: "All Posts",
      view: "overview",
    },
    {
      icon: <BarChart3 size={20} />,
      label: "Statistics",
      view: "statistics",
    },
    {
      icon: <Tag size={20} />,
      label: "Tag Management",
      view: "tags",
    },
    {
      icon: <FileText size={20} />,
      label: "Pending Posts",
      view: "pending",
    },
    {
      icon: <Flag size={20} />,
      label: "Reported Comments",
      view: "reported",
    },
    {
      icon: <XCircle size={20} />,
      label: "Rejected Posts",
      view: "rejected",
    },
  ];

  return (
    <aside className={styles.sidebar}>
      <div className={styles.sidebarHeader}>
        <div className={styles.logo}>
          <GraduationCap
            size={28}
            color="#38bdf8"
            className={styles.logoIcon}
          />
          <span className={styles.logoText}>IELTSPhobic</span>
        </div>
      </div>

      <nav className={styles.sidebarNav}>
        {menuItems.map((item, index) => (
          <button
            key={index}
            className={`${styles.navItem} ${
              currentView === item.view ? styles.active : ""
            }`}
            onClick={() => onViewChange(item.view)}
          >
            {item.icon && <span className={styles.icon}>{item.icon}</span>}
            <span className={styles.label}>{item.label}</span>
          </button>
        ))}
      </nav>

      <div className={styles.sidebarFooter}>
        <Cloud size={30} color="#6c757d" className={styles.cloudIcon} />
        <Wallet size={28} color="#28a745" className={styles.moneySymbols} />
        <p className={styles.footerText}>
          Give your money awesome space in cloud
        </p>
      </div>
    </aside>
  );
}

