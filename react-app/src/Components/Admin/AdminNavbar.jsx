import React from "react";
import Sidebar from "../Layout/Sidebar.jsx";
import styles from "./AdminNavbar.module.css";
import {
    Users,
    BookOpen,
    DollarSign,
    Shield,
} from "lucide-react";

const menuItems = [
  { icon: <Users size={20} />, label: "Users", path: "/admin/users" },
  { icon: <BookOpen size={20} />, label: "Exams", path: "/admin/exam" },
  {
    icon: <DollarSign size={20} />,
    label: "Transactions",
    path: "/admin/transactions",
  },

  {
    icon: <Shield size={20} />,
    label: "Moderator View",
    path: "/moderator/dashboard",
  },  
];

export default function AdminNavbar() {
  return (
    <aside className={styles.sidebar}>
      <Sidebar menuItems={menuItems} />
    </aside>
  );
}
