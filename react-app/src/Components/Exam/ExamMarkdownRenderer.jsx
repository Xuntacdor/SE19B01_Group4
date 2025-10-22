import React from "react";
import { marked } from "marked";
import "./ExamMarkdownRenderer.css";

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function splitBlocks(md) {
  const lines = md.split(/\r?\n/);
  const blocks = [];
  let buffer = [];
  let qBuffer = [];
  let inQuestion = false;

  const flushBuffer = () => {
    if (buffer.length) {
      blocks.push({ type: "markdown", text: buffer.join("\n") });
      buffer = [];
    }
  };
  const flushQuestion = () => {
    if (qBuffer.length) {
      blocks.push({ type: "question", lines: [...qBuffer] });
      qBuffer = [];
      inQuestion = false;
    }
  };

  for (const line of lines) {
    if (line.includes("[!num]")) {
      flushBuffer();
      if (inQuestion) flushQuestion();
      inQuestion = true;
      qBuffer = [line];
    } else if (inQuestion) {
      qBuffer.push(line);
    } else {
      buffer.push(line);
    }
  }
  if (inQuestion) flushQuestion();
  flushBuffer();
  return blocks;
}

marked.setOptions({
  gfm: true,
  breaks: true,
  mangle: false,
  headerIds: false,
});

function processQuestionBlock(lines, qIndex, showAnswers, readingId = 0) {
  let text = lines.join("\n");

  // 🟢 Text inputs
  text = text.replace(/\[T\*([^\]]+)\]/g, (_, ans) =>
    showAnswers
      ? `<input type="text" value="${escapeHtml(
          ans
        )}" readonly class="inlineTextbox answerFilled" />`
      : `<input type="text" class="inlineTextbox" name="${readingId}_q${qIndex}_text" />`
  );
  text = text.replace(
    /\[T\]/g,
    `<input type="text" class="inlineTextbox" name="${readingId}_q${qIndex}_text" />`
  );

  // 🟢 Dropdowns ([D] ... [/D])
  const choiceRegex = /\[([* ])\]\s*([^\n\[]+)/g;
  const dropdownRegex = /\[D\]([\s\S]*?)\[\/D\]/g;
  text = text.replace(dropdownRegex, (_, inner) => {
    const options = [...inner.matchAll(choiceRegex)].map((m) => ({
      correct: m[1] === "*",
      text: m[2].trim(),
    }));
    const longest = Math.min(
      Math.max(...options.map((o) => o.text.length)) + 2,
      30
    );
    const html =
  `<select name="${readingId}_q${qIndex}" class="dropdownInline" style="width:${longest}ch" ${
    showAnswers ? "disabled" : ""
  }>
    <option value="" disabled selected hidden>Select...</option>` +
      options
        .map(
          (o) =>
            `<option value="${escapeHtml(o.text)}"${
              showAnswers && o.correct ? " selected" : ""
            }>${escapeHtml(o.text)}</option>`
        )
        .join("") +
      "</select>";
    return html;
  });

  // 🟢 Radio / Checkbox
  const choiceRegex2 = /\[([* ])\]\s*([^\n\[]+)/g;
  const outsideDropdown = text.replace(/\[D\][\s\S]*?\[\/D\]/g, "");
  const correctCount = (outsideDropdown.match(/\[\*\]/g) || []).length;
  const isMulti = correctCount > 1;
  const type = isMulti ? "checkbox" : "radio";

  text = text.replace(choiceRegex2, (match, mark, label) => {
    const value = escapeHtml(label.trim());
    const checked = showAnswers && mark === "*" ? "checked" : "";
    const limitAttr = isMulti
      ? `data-limit="${correctCount}" data-group="${readingId}_q${qIndex}"`
      : "";
    return `<label class="choiceItem">
      <input type="${type}" name="${readingId}_q${qIndex}" value="${value}" ${limitAttr} ${checked} ${
      showAnswers ? "disabled" : ""
    }/>
      ${value}
    </label>`;
  });

  // 🟢 Question number
  text = text.replace(
    /\[!num\]/g,
    `<span class="numberIndex">Q${qIndex}.</span>`
  );

  return marked.parse(text);
}

export default function ExamMarkdownRenderer({
  markdown = "",
  showAnswers = false,
  userAnswers = [],
  correctAnswers = [],
  readingId = 0,
}) {
  const blocks = splitBlocks(markdown);
  let qCounter = 0;

  const html = blocks
    .map((b) => {
      if (b.type === "markdown") return marked.parse(b.text);
      if (b.type === "question") {
        qCounter++;
        let html = processQuestionBlock(b.lines, qCounter, showAnswers, readingId);

        if (showAnswers) {
          const user = userAnswers[qCounter - 1] || "_";
          const correct = correctAnswers[qCounter - 1] || "_";
          const isCorrect =
            user.trim().toLowerCase() === correct.trim().toLowerCase();

          html = html.replace(
            /(<input|<select)/,
            `$1 style="border:2px solid ${
              isCorrect ? "#22c55e" : user === "_" ? "#9ca3af" : "#ef4444"
            }; background-color:${
              isCorrect ? "#dcfce7" : user === "_" ? "#f3f4f6" : "#fee2e2"
            }" readonly value="${user}" title="Correct: ${correct}" `
          );
        }

        return html;
      }
      return "";
    })
    .join("\n");

  return <div className="renderer" dangerouslySetInnerHTML={{ __html: html }} />;
}

// 🟢 Export helper that generates HTML + correct answers list
export function renderMarkdownToHtmlAndAnswers(markdown, readingId = 0) {
  const blocks = splitBlocks(markdown);
  let htmlOutput = "";
  let allAnswers = [];
  let qCounter = 0;

  blocks.forEach((b) => {
    if (b.type === "markdown") {
      htmlOutput += marked.parse(b.text);
      return;
    }

    qCounter++;
    const full = b.lines.join("\n");

    // Extract correct answers
    const textAnswers = [...full.matchAll(/\[T\*([^\]]+)\]/g)].map((m) =>
      m[1].trim()
    );
    const dropdownAnswers = [...full.matchAll(/\[D\]([\s\S]*?)\[\/D\]/g)].flatMap(
      ([, inner]) =>
        [...inner.matchAll(/\[\*\]\s*([^\n\[]+)/g)].map((m) => m[1].trim())
    );
    const outsideDropdown = full.replace(/\[D\][\s\S]*?\[\/D\]/g, "");
    const radioAnswers = [...outsideDropdown.matchAll(/\[\*\]\s*([^\n\[]+)/g)].map(
      (m) => m[1].trim()
    );

    allAnswers.push(...textAnswers, ...dropdownAnswers, ...radioAnswers);
    htmlOutput += processQuestionBlock(b.lines, qCounter, false, readingId);
  });

  return { html: htmlOutput, answers: allAnswers };
}
