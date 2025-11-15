import React, { useRef, useCallback } from "react";
import styles from "./ContentTextBox.module.css";
import MarkdownToolbar from "./MarkdownToolbar";

export default function ContentTextBox({
  label,
  value,
  onChange,
  rows = 6,
  placeholder,
}) {
  const textareaRef = useRef(null);
  const text = value || "";

  const applyTransform = useCallback(
    (action) => {
      const textarea = textareaRef.current;

      if (!textarea) {
        // Fallback: append at end
        let appended = text;
        switch (action) {
          case "bold":
            appended += "**text**";
            break;
          case "italic":
            appended += "*text*";
            break;
          case "question":
            appended += "[!num] ";
            break;
          case "textInput":
            appended += "[T]";
            break;
          case "mcqCorrect":
            appended += "[*] Option";
            break;
          case "mcqWrong":
            appended += "[ ] Option";
            break;
          case "dropdown":
            appended += "[D]\n[*] Option A\n[ ] Option B\n[/D]";
            break;
          case "explanation":
            appended += "[H]\nExplanation here...\n[/H]";
            break;
          default:
            break;
        }
        onChange(appended);
        return;
      }

      const start = textarea.selectionStart ?? text.length;
      const end = textarea.selectionEnd ?? text.length;
      const selected = text.slice(start, end);
      const before = text.slice(0, start);
      const after = text.slice(end);

      let insert = "";
      let newSelectionStart = start;
      let newSelectionEnd = end;

      const wrapSimple = (left, right, placeholder) => {
        if (selected) {
          const isWrapped =
            selected.startsWith(left) && selected.endsWith(right);
          if (isWrapped) {
            const inner = selected.slice(left.length, selected.length - right.length);
            insert = inner;
            newSelectionStart = start;
            newSelectionEnd = start + inner.length;
          } else {
            insert = left + selected + right;
            newSelectionStart = start;
            newSelectionEnd = start + insert.length;
          }
        } else {
          const content = placeholder || "";
          insert = left + content + right;
          if (content) {
            newSelectionStart = start + left.length;
            newSelectionEnd = start + left.length + content.length;
          } else {
            newSelectionStart = start + insert.length;
            newSelectionEnd = newSelectionStart;
          }
        }
      };

      const toMcqLines = (marker) => {
        if (selected.trim()) {
          const lines = selected.split(/\r?\n/);
          const formatted = lines
            .map((line) => {
              const trimmed = line.trim();
              if (!trimmed) return "";
              const cleaned = trimmed.replace(/^\[(\*| )\]\s*/, "");
              return `${marker} ${cleaned}`;
            })
            .join("\n");
          insert = formatted;
          newSelectionStart = start;
          newSelectionEnd = start + insert.length;
        } else {
          insert = `${marker} Option`;
          newSelectionStart = start + insert.length;
          newSelectionEnd = newSelectionStart;
        }
      };

      switch (action) {
        case "bold":
          wrapSimple("**", "**", "text");
          break;

        case "italic":
          wrapSimple("*", "*", "text");
          break;

        case "question":
          insert = selected
            ? `[!num] ${selected}`
            : "[!num] ";
          newSelectionStart = start + insert.length;
          newSelectionEnd = newSelectionStart;
          break;

        case "textInput":
          if (selected) {
            insert = `[T*${selected}]`;
            newSelectionStart = start;
            newSelectionEnd = start + insert.length;
          } else {
            insert = "[T]";
            newSelectionStart = start + insert.length;
            newSelectionEnd = newSelectionStart;
          }
          break;

        case "mcqCorrect":
          toMcqLines("[*]");
          break;

        case "mcqWrong":
          toMcqLines("[ ]");
          break;

        case "dropdown":
          if (selected.trim()) {
            const lines = selected
              .split(/\r?\n/)
              .map((l) => l.trim())
              .filter((l) => l.length > 0);

            if (lines.length > 0) {
              const first = lines[0].replace(/^\[(\*| )\]\s*/, "");
              const rest = lines.slice(1).map((l) =>
                l.replace(/^\[(\*| )\]\s*/, "")
              );

              const inner = [
                `[*] ${first}`,
                ...rest.map((l) => `[ ] ${l}`),
              ].join("\n");

              insert = `[D]\n${inner}\n[/D]`;
            } else {
              insert = "[D]\n[*] Option A\n[ ] Option B\n[/D]";
            }
          } else {
            insert = "[D]\n[*] Option A\n[ ] Option B\n[/D]";
          }
          newSelectionStart = start + insert.length;
          newSelectionEnd = newSelectionStart;
          break;

        case "explanation":
          if (selected) {
            insert = `[H]\n${selected}\n[/H]`;
          } else {
            insert = "[H]\nExplanation here...\n[/H]";
          }
          newSelectionStart = start + insert.length;
          newSelectionEnd = newSelectionStart;
          break;

        default:
          insert = selected || "";
          break;
      }

      const newText = before + insert + after;
      onChange(newText);

      requestAnimationFrame(() => {
        if (textareaRef.current) {
          textareaRef.current.focus();
          textareaRef.current.selectionStart = newSelectionStart;
          textareaRef.current.selectionEnd = newSelectionEnd;
        }
      });
    },
    [text, onChange]
  );

  const handleKeyDown = (e) => {
    if (e.ctrlKey && !e.altKey) {
      if (e.key === "b" || e.key === "B") {
        e.preventDefault();
        applyTransform("bold");
        return;
      }
      if (e.key === "i" || e.key === "I") {
        e.preventDefault();
        applyTransform("italic");
        return;
      }
      if (e.key === "e" || e.key === "E") {
        e.preventDefault();
        applyTransform("explanation");
        return;
      }
      if (e.shiftKey && (e.key === "d" || e.key === "D")) {
        e.preventDefault();
        applyTransform("dropdown");
        return;
      }
      if (e.shiftKey && (e.key === "q" || e.key === "Q")) {
        e.preventDefault();
        applyTransform("question");
        return;
      }
    }
  };

  return (
    <div className={styles.group}>
      <label>{label}</label>

      <MarkdownToolbar onAction={applyTransform} />

      <textarea
        ref={textareaRef}
        value={text}
        onChange={(e) => onChange(e.target.value)}
        rows={rows}
        placeholder={placeholder}
        onKeyDown={handleKeyDown}
      />
    </div>
  );
}
