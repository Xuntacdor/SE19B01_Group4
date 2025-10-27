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

// 🟢 Split markdown into content & question blocks
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

// 🟢 Render question block (convert markdown syntax to HTML)
function processQuestionBlock(lines, qIndex, showAnswers, readingId = 0) {
  let text = lines.join("\n");

  // Use "X" placeholder when readingId not yet known (e.g., AddReading preview)
  const safeId = readingId && readingId > 0 ? readingId : "X";

  // Text inputs
  text = text.replace(/\[T\*([^\]]+)\]/g, (_, ans) => {
    const width = Math.min(Math.max(ans.length + 5, 10), 30);
    return showAnswers
      ? `<input type="text" value="${escapeHtml(
          ans
        )}" readonly class="inlineTextbox answerFilled" style="width:${width}ch;" />`
      : `<input type="text" class="inlineTextbox" name="${safeId}_q${qIndex}" style="width:${width}ch;" />`;
  });

  text = text.replace(/\[T\]/g, () => {
    const width = 15;
    return `<input type="text" class="inlineTextbox" name="${safeId}_q${qIndex}" style="width:${width}ch;" />`;
  });

  // Dropdowns
  const choiceRegex = /\[([* ])\]\s*([^\n\[]+)/g;
  const dropdownRegex = /\[D\]([\s\S]*?)\[\/D\]/g;
  text = text.replace(dropdownRegex, (_, inner) => {
    const options = [...inner.matchAll(choiceRegex)].map((m) => ({
      correct: m[1] === "*",
      text: m[2].trim(),
    }));
    const longest = Math.min(Math.max(...options.map((o) => o.text.length)) + 5, 30);
    return (
      `<select name="${safeId}_q${qIndex}" class="dropdownInline" style="width:${longest}ch" ${
        showAnswers ? "disabled" : ""
      }>` +
      `<option value="" disabled selected hidden></option>` +
      options
        .map(
          (o) =>
            `<option value="${escapeHtml(o.text)}"${
              showAnswers && o.correct ? " selected" : ""
            }>${escapeHtml(o.text)}</option>`
        )
        .join("") +
      "</select>"
    );
  });

  // Radio/checkbox
  const choiceRegex2 = /\[([* ])\]\s*([^\n\[]+)/g;
  const outsideDropdown = text.replace(/\[D\][\s\S]*?\[\/D\]/g, "");
  const correctCount = (outsideDropdown.match(/\[\*\]/g) || []).length;
  const isMulti = correctCount > 1;
  const type = isMulti ? "checkbox" : "radio";

  text = text.replace(choiceRegex2, (match, mark, label) => {
    const value = escapeHtml(label.trim());
    const checked = showAnswers && mark === "*" ? "checked" : "";
    const limitAttr = isMulti
      ? `data-limit="${correctCount}" data-group="${safeId}_q${qIndex}"`
      : "";
    return `<label class="choiceItem">
      <input type="${type}" name="${safeId}_q${qIndex}" value="${value}" ${limitAttr} ${checked} ${
      showAnswers ? "disabled" : ""
    }/>
      ${value}
    </label>`;
  });

  // Question number
  text = text.replace(/\[!num\]/g, `<span class="numberIndex">Q${qIndex}.</span>`);

  return marked.parse(text);
}

// 🟢 Export helper for generating correct answers
export function renderMarkdownToHtmlAndAnswers(markdown, readingId = 0) {
  const blocks = splitBlocks(markdown);
  let htmlOutput = "";
  const allAnswers = {};
  let qCounter = 0;
  const safeId = readingId && readingId > 0 ? readingId : "X";

  blocks.forEach((b) => {
    if (b.type === "markdown") {
      htmlOutput += marked.parse(b.text);
      return;
    }

    qCounter++;
    const full = b.lines.join("\n");

    const textAnswers = [...full.matchAll(/\[T\*([^\]]+)\]/g)].map((m) => m[1].trim());
    const dropdownAnswers = [...full.matchAll(/\[D\]([\s\S]*?)\[\/D\]/g)].flatMap(([, inner]) =>
      [...inner.matchAll(/\[\*\]\s*([^\n\[]+)/g)].map((m) => m[1].trim())
    );
    const outsideDropdown = full.replace(/\[D\][\s\S]*?\[\/D\]/g, "");
    const radioAnswers = [...outsideDropdown.matchAll(/\[\*\]\s*([^\n\[]+)/g)].map((m) =>
      m[1].trim()
    );

    const answersForThisQ = [...textAnswers, ...dropdownAnswers, ...radioAnswers];
    allAnswers[`${safeId}_q${qCounter}`] =
      answersForThisQ.length > 1 ? answersForThisQ : answersForThisQ[0] || "_";

    htmlOutput += processQuestionBlock(b.lines, qCounter, false, readingId);
  });

  return { html: htmlOutput, answers: allAnswers };
}

// 🟢 Main Component
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
        return processQuestionBlock(b.lines, qCounter, showAnswers, readingId);
      }
      return "";
    })
    .join("\n");

  return <div className="renderer" dangerouslySetInnerHTML={{ __html: html }} />;
}
