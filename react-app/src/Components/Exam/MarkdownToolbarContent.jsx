// Components/Common/MarkdownToolbarContent.jsx
import React from "react";
import styles from "./MarkdownToolbar.module.css";
import { Bold, Italic, HelpCircle } from "lucide-react";

/**
 * Content box only uses:
 *  - bold
 *  - italic
 *  - explanation [H] ... [/H]
 */
export default function MarkdownToolbarContent({ onAction }) {
  const buttons = [
    { id: "bold", icon: <Bold size={16} />, label: "Bold (**text**)" },
    { id: "italic", icon: <Italic size={16} />, label: "Italic (*text*)" },
    {
      id: "explanation",
      icon: <HelpCircle size={16} />,
      label: "Explanation [H]…[/H]",
    },
  ];

  return (
    <div className={styles.toolbar}>
      <div className={styles.toolbarGroup}>
        <div className={styles.toolbarButtons}>
          {buttons.map((b) => (
            <button
              key={b.id}
              type="button"
              className={styles.btn}
              onClick={() => onAction && onAction(b.id)}
              title={b.label}
            >
              {b.icon}
            </button>
          ))}
        </div>
        <div className={styles.toolbarLabel}>Formatting</div>
      </div>
    </div>
  );
}
