// Components/Common/MarkdownToolbar.jsx
import React from "react";
import styles from "./MarkdownToolbar.module.css";
import {
  Bold,
  Italic,
  HelpCircle,
  ListChecks,
  List,
  Type,
  PlusSquare,
  SquareCheckBig,
  Square,
} from "lucide-react";

/**
 * onAction(action: string)
 * action ∈ [
 *  "bold", "italic",
 *  "question",
 *  "textInput",
 *  "mcqCorrect", "mcqWrong",
 *  "dropdown",
 *  "explanation"
 * ]
 */
export default function MarkdownToolbar({ onAction }) {
  const groups = [
    {
      label: "Formatting",
      buttons: [
        {
          id: "bold",
          icon: <Bold size={16} />,
          label: "Bold (**text**) – Ctrl+B",
        },
        {
          id: "italic",
          icon: <Italic size={16} />,
          label: "Italic (*text*) – Ctrl+I",
        },
      ],
    },
    {
      label: "Question",
      buttons: [
        {
          id: "question",
          icon: <PlusSquare size={16} />,
          label: "Insert [!num] – Ctrl+Shift+Q",
        },
        {
          id: "textInput",
          icon: <Type size={16} />,
          label: "Text input [T] / [T*answer]",
        },
      ],
    },
    {
      label: "Options",
      buttons: [
        {
          id: "mcqCorrect",
          icon: <SquareCheckBig size={16} />,
          label: "Correct option [*]",
        },
        {
          id: "mcqWrong",
          icon: <Square size={16} />,
          label: "Wrong option [ ]",
        },
        {
          id: "dropdown",
          icon: <List size={16} />,
          label: "Dropdown [D]…[/D] – Ctrl+Shift+D",
        },
      ],
    },
    {
      label: "Explanation",
      buttons: [
        {
          id: "explanation",
          icon: <HelpCircle size={16} />,
          label: "Explanation [H]…[/H] – Ctrl+E",
        },
      ],
    },
  ];

  return (
    <div className={styles.toolbar}>
      {groups.map((g, gi) => (
        <React.Fragment key={g.label}>
          <div className={styles.toolbarGroup}>
            <div className={styles.toolbarButtons}>
              {g.buttons.map((b) => (
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
            <div className={styles.toolbarLabel}>{g.label}</div>
          </div>

          {gi !== groups.length - 1 && (
            <div className={styles.toolbarSeparator} />
          )}
        </React.Fragment>
      ))}
    </div>
  );
}
