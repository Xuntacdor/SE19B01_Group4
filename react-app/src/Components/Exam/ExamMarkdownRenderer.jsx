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

// ================= marked config =================
marked.setOptions({
  gfm: true,
  breaks: true,      // keep your "dropdown-style" newline behavior
  mangle: false,
  headerIds: false,
});

// ================= split logic (unchanged) =================
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

// ================= helpers =================
function normalize(val) {
  if (Array.isArray(val)) return val.map((x) => String(x).trim().toLowerCase());
  if (val == null) return [];
  return [String(val).trim().toLowerCase()];
}

function normalizeUser(raw) {
  if (raw == null) return { arr: [], isMissing: true };

  if (Array.isArray(raw)) {
    const cleaned = raw.map((v) => String(v).trim()).filter((v) => v !== "");
    if (cleaned.length === 0) return { arr: [], isMissing: true };
    if (cleaned.length === 1 && cleaned[0] === "_")
      return { arr: [], isMissing: true };
    return { arr: cleaned.map((v) => v.toLowerCase()), isMissing: false };
  }

  const s = String(raw).trim();
  if (s === "" || s === "_") return { arr: [], isMissing: true };
  return { arr: [s.toLowerCase()], isMissing: false };
}

// Post-process the final HTML of a QUESTION block and turn [H*id]...[/H] into <details> ... </details>.
// We do this AFTER markdown runs so nothing escapes as literal text.
function applyQuestionExplanationsHTML(html, showAnswers) {
  // If we’re on the exam page, explanations should not render at all.
  if (!showAnswers) {
    return html.replace(/\[H(?:\*([^\]]*))?\]([\s\S]*?)\[\/H\]/g, "");
  }

  // On result page, inject details/summary blocks.
  return html.replace(/\[H(?:\*([^\]]*))?\]([\s\S]*?)\[\/H\]/g, (_, rawId, inner) => {
    const hid = (rawId || "").trim();
    // Note: inner is already HTML (line breaks became <br/>, etc.) because this runs after marked.parse
    return `
      <details class="explainBlock" ${hid ? `data-hid="${escapeHtml(hid)}"` : ""}>
        <summary class="explainBtn" ${hid ? `data-hid="${escapeHtml(hid)}"` : ""}>
          Explanation${hid ? ` #${escapeHtml(hid)}` : ""}
        </summary>
        <div class="explainBody">${inner}</div>
      </details>
    `;
  });
}

// ================= per-question renderer =================
function processQuestionBlock(
  lines,
  qIndex,
  showAnswers,
  skillId = 0,
  userAnsObj = {},
  correctAnsObj = {}
) {
  let text = lines.join("\n");

  const safeId = skillId && skillId > 0 ? skillId : "X";
  const key = `${safeId}_q${qIndex}`;

  const userNorm = normalizeUser(userAnsObj?.[key]);
  const cArr = normalize(correctAnsObj?.[key]);
  const uSet = new Set(userNorm.arr);
  const cSet = new Set(cArr);

  // 🟢 Text inputs [T*answer]
  text = text.replace(/\[T\*([^\]]+)\]/g, (_, ans) => {
    const width = Math.min(Math.max(ans.length + 5, 10), 30);
    if (!showAnswers) {
      return `<input type="text" class="inlineTextbox" name="${safeId}_q${qIndex}" style="width:${width}ch;" />`;
    }
    const uRaw = userAnsObj?.[key];
    const uFirst = Array.isArray(uRaw) ? uRaw[0] ?? "" : uRaw ?? "";
    const uFirstTrim = String(uFirst).trim();
    const isMissing = uFirstTrim === "" || uFirstTrim === "_";
    const ok =
      !isMissing &&
      uFirstTrim.toLowerCase() === String(ans).trim().toLowerCase();

    const statusClass = isMissing ? "qa-missing" : ok ? "qa-right" : "qa-wrong";
    const title = isMissing
      ? "Your answer: (blank)"
      : `Your answer: ${escapeHtml(uFirstTrim)}`;

    return `<input type="text" value="${escapeHtml(
      ans
    )}" readonly class="inlineTextbox answerFilled ${statusClass}" title="${title}" style="width:${width}ch;" />`;
  });

  // [T] -> plain input on exam, disabled on review
  text = text.replace(/\[T\]/g, () => {
    const width = 15;
    if (!showAnswers) {
      return `<input type="text" class="inlineTextbox" name="${safeId}_q${qIndex}" style="width:${width}ch;" />`;
    }
    return `<input type="text" class="inlineTextbox" name="${safeId}_q${qIndex}" style="width:${width}ch;" disabled />`;
  });

  // 🟢 Dropdowns [D] ... [/D]
  const choiceRegex = /\[([* ])\]\s*([^\n\[]+)/g;
  const dropdownRegex = /\[D\]([\s\S]*?)\[\/D\]/g;
  text = text.replace(dropdownRegex, (_, inner) => {
    const options = [...inner.matchAll(choiceRegex)].map((m) => ({
      correct: m[1] === "*",
      text: m[2].trim(),
    }));

    const userPickRaw = userAnsObj?.[key];
    const userPickNorm = normalizeUser(userPickRaw);
    const userPick = userPickNorm.arr[0] || "";
    const correctPick =
      (options.find((o) => o.correct)?.text ?? "").toLowerCase();

    let selectStatus = "";
    if (showAnswers) {
      if (userPickNorm.isMissing && correctPick) selectStatus = "qa-missing";
      else if (!userPickNorm.isMissing && userPick === correctPick)
        selectStatus = "qa-right";
      else if (!userPickNorm.isMissing && userPick !== correctPick)
        selectStatus = "qa-wrong";
    }

    const longest = Math.min(
      Math.max(...options.map((o) => o.text.length)) + 5,
      30
    );
    return (
      `<select name="${safeId}_q${qIndex}" class="dropdownInline ${selectStatus}" style="width:${longest}ch" ${
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

  // 🟢 Radio / Checkbox group
  const choiceRegex2 = /\[([* ])\]\s*([^\n\[]+)/g;
  const outsideDropdown = text.replace(/\[D\][\s\S]*?\[\/D\]/g, "");
  const correctCount = (outsideDropdown.match(/\[\*\]/g) || []).length;
  const isMulti = correctCount > 1;
  const type = isMulti ? "checkbox" : "radio";

  const allChoiceLowers = [...outsideDropdown.matchAll(choiceRegex2)].map((m) =>
    String(m[2]).trim().toLowerCase()
  );
  const hasChoices = allChoiceLowers.length > 0;
  const applyGroupMissing = showAnswers && hasChoices && userNorm.isMissing;

  text = text.replace(choiceRegex2, (match, mark, label) => {
    const value = String(label).trim();
    const vKey = value.toLowerCase();

    let hl = "";
    if (showAnswers) {
      if (applyGroupMissing) {
        hl = "qa-missing";
      } else if (cSet.has(vKey)) {
        hl = "qa-right";
      } else if (uSet.has(vKey) && !cSet.has(vKey)) {
        hl = "qa-wrong";
      }
    }

    const checked = showAnswers && mark === "*" ? "checked" : "";
    const limitAttr = isMulti
      ? `data-limit="${correctCount}" data-group="${safeId}_q${qIndex}"`
      : "";

    return `<label class="choiceItem ${hl}">
      <input type="${type}" name="${safeId}_q${qIndex}" value="${escapeHtml(
      value
    )}" ${limitAttr} ${checked} ${showAnswers ? "disabled" : ""}/>
      ${escapeHtml(value)}
    </label>`;
  });

  // Number
  text = text.replace(/\[!num\]/g, `<span class="numberIndex">Q${qIndex}.</span>`);

  // First, convert all markdown to HTML:
  let html = marked.parse(text);

  // Then, replace the [H*id]...[/H] markers (now living inside that HTML) with the explanation UI (or remove on exam page)
  html = applyQuestionExplanationsHTML(html, showAnswers);

  return html;
}

// ================= main =================
export default function ExamMarkdownRenderer({
  markdown = "",
  showAnswers = false,
  userAnswers = [],
  correctAnswers = [],
  skillId = 0,
}) {
  const blocks = splitBlocks(markdown);
  let qCounter = 0;

  const userAnsObj =
    userAnswers?.find((x) => x?.SkillId === skillId)?.Answers || {};
  const correctAnsObj =
    correctAnswers?.find((x) => x?.SkillId === skillId)?.Answers || {};

  const html = blocks
    .map((b) => {
      if (b.type === "markdown") return marked.parse(b.text);
      if (b.type === "question") {
        qCounter++;
        return processQuestionBlock(
          b.lines,
          qCounter,
          showAnswers,
          skillId,
          userAnsObj,
          correctAnsObj
        );
      }
      return "";
    })
    .join("\n");

  return <div className="renderer" dangerouslySetInnerHTML={{ __html: html }} />;
}

// Exported helper unchanged in signature/behavior (no [H] logic here)
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

    const textAnswers = [...full.matchAll(/\[T\*([^\]]+)\]/g)].map((m) =>
      m[1].trim()
    );
    const dropdownAnswers = [
      ...full.matchAll(/\[D\]([\s\S]*?)\[\/D\]/g),
    ].flatMap(([, inner]) =>
      [...inner.matchAll(/\[\*\]\s*([^\n\[]+)/g)].map((m) => m[1].trim())
    );
    const outsideDropdown = full.replace(/\[D\][\s\S]*?\[\/D\]/g, "");
    const radioAnswers = [
      ...outsideDropdown.matchAll(/\[\*\]\s*([^\n\[]+)/g),
    ].map((m) => m[1].trim());

    const answersForThisQ = [
      ...textAnswers,
      ...dropdownAnswers,
      ...radioAnswers,
    ];
    allAnswers[`${safeId}_q${qCounter}`] =
      answersForThisQ.length > 1 ? answersForThisQ : answersForThisQ[0] || "_";

    // For exam preview, we do not inject explanations
    htmlOutput += marked.parse(full);
  });

  return { html: htmlOutput, answers: allAnswers };
}
