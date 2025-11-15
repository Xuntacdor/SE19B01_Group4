// Components/Common/ContentTextBox.jsx
import React, { useRef, useCallback, useEffect } from "react";
import styles from "./ContentTextBox.module.css";
import MarkdownToolbarContent from "./MarkdownToolbarContent";

export default function ContentTextBox({
  label,
  value,
  onChange,
  rows = 6,
  placeholder,
}) {
  const textareaRef = useRef(null);
  const text = value || "";

  const historyRef = useRef({
    stack: [text],
    index: 0,
  });

  useEffect(() => {
    const h = historyRef.current;
    const current = h.stack[h.index];

    if (current === text) return;

    if (h.index < h.stack.length - 1) {
      h.stack = h.stack.slice(0, h.index + 1);
    }

    h.stack.push(text);
    h.index = h.stack.length - 1;
  }, [text]);

  const applyTransform = useCallback(
    (action) => {
      const textarea = textareaRef.current;

      if (!textarea) {
        let appended = text;
        if (action === "bold") appended += "**text**";
        if (action === "italic") appended += "*text*";
        if (action === "explanation")
          appended += "[H]\nExplanation here...\n[/H]";
        onChange(appended);
        return;
      }

      const start = textarea.selectionStart ?? text.length;
      const end = textarea.selectionEnd ?? text.length;
      const selected = text.slice(start, end);

      const before = text.slice(0, start);
      const after = text.slice(end);

      let insert = "";
      let newStart = start;
      let newEnd = end;

      const wrap = (left, right, placeholder = "") => {
        if (selected) {
          const isWrapped =
            selected.startsWith(left) && selected.endsWith(right);
          if (isWrapped) {
            const inner = selected.slice(
              left.length,
              selected.length - right.length
            );
            insert = inner;
            newStart = start;
            newEnd = start + inner.length;
          } else {
            insert = left + selected + right;
            newStart = start;
            newEnd = start + insert.length;
          }
        } else {
          insert = left + placeholder + right;
          newStart = start + left.length;
          newEnd = newStart + placeholder.length;
        }
      };

      if (action === "bold") wrap("**", "**", "text");
      if (action === "italic") wrap("*", "*", "text");

      if (action === "explanation") {
        if (selected) {
          insert = `[H]\n${selected}\n[/H]`;
        } else {
          insert = "[H]\nExplanation here...\n[/H]";
        }
        newStart = start + insert.length;
        newEnd = newStart;
      }

      const newText = before + insert + after;
      onChange(newText);

      requestAnimationFrame(() => {
        if (textareaRef.current) {
          textareaRef.current.focus();
          textareaRef.current.selectionStart = newStart;
          textareaRef.current.selectionEnd = newEnd;
        }
      });
    },
    [text, onChange]
  );

  const handleKeyDown = (e) => {
    if (e.ctrlKey && !e.altKey) {
      const key = e.key.toLowerCase();
      const h = historyRef.current;

      // Undo: Ctrl+Z
      if (key === "z" && !e.shiftKey) {
        e.preventDefault();
        if (h.index > 0) {
          h.index -= 1;
          onChange(h.stack[h.index]);
        }
        return;
      }

      // Redo: Ctrl+Y or Ctrl+Shift+Z
      if (key === "y" || (key === "z" && e.shiftKey)) {
        e.preventDefault();
        if (h.index < h.stack.length - 1) {
          h.index += 1;
          onChange(h.stack[h.index]);
        }
        return;
      }

      // Formatting shortcuts
      if (key === "b") {
        e.preventDefault();
        applyTransform("bold");
        return;
      }
      if (key === "i") {
        e.preventDefault();
        applyTransform("italic");
        return;
      }
      if (key === "e") {
        e.preventDefault();
        applyTransform("explanation");
        return;
      }
    }
  };

  return (
    <div className={styles.group}>
      <label>{label}</label>

      <MarkdownToolbarContent onAction={applyTransform} />

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
