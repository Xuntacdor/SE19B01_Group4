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
 *  action ∈ [
 *   "bold", "italic",
 *   "question",
 *   "textInput",
 *   "mcqCorrect", "mcqWrong",
 *   "dropdown",
 *   "explanation"
 *  ]
 */
export default function MarkdownToolbar({ onAction }) {
  const buttons = [
    {
      id: "bold",
      icon: <Bold size={16} />,
      label: "Bold (**text**)  – Ctrl+B",
    },
    {
      id: "italic",
      icon: <Italic size={16} />,
      label: "Italic (*text*) – Ctrl+I",
    },
    {
      id: "question",
      icon: <PlusSquare size={16} />,
      label: "Insert question marker [!num] – Ctrl+Shift+Q",
    },
    {
      id: "textInput",
      icon: <Type size={16} />,
      label: "Insert text input [T] or [T*answer]",
    },
    {
      id: "mcqCorrect",
      icon: <SquareCheckBig size={16} />,
      label: "Correct MCQ option [*] – use with selected lines",
    },
    {
      id: "mcqWrong",
      icon: <Square size={16} />,
      label: "Wrong MCQ option [ ] – use with selected lines",
    },
    {
      id: "dropdown",
      icon: <List size={16} />,
      label: "Dropdown [D] ... [/D] – Ctrl+Shift+D",
    },
    {
      id: "explanation",
      icon: <HelpCircle size={16} />,
      label: "Explanation [H]...[/H] – Ctrl+E",
    },
  ];

  return (
    <div className={styles.toolbar}>
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
  );
}
