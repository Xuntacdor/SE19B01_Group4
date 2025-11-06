// ReadingResultPage.jsx
import React, { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as ReadingApi from "../../Services/ReadingApi";
import * as ExamApi from "../../Services/ExamApi";
import { marked } from "marked";
import styles from "./ReadingResultPage.module.css";
import ExamMarkdownRenderer from "../../Components/Exam/ExamMarkdownRenderer";

// ---------- Markdown config for the PASSAGE on the RESULT page ----------
marked.setOptions({
  gfm: true,
  breaks: true, // single newlines -> <br>
  mangle: false,
  headerIds: false,
});

function escapeHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

/**
 * Render the whole passage as Markdown BUT:
 * - Wrap each [H*id]...[/H] region in <div|span class="passageTarget" data-hid="...">...</div|span>
 * - Parse the INSIDE of those regions with Markdown too
 * This keeps anchors stable for syncing with explanations (block-safe).
 */
function passageWithAnchorsToHtml(raw) {
  if (!raw) return "";

  const re = /\[H(?:\*([^\]]*))?\]([\s\S]*?)\[\/H\]/g;
  let out = "";
  let last = 0;
  let m;

  const hasBlock = (html) =>
    /<(p|ul|ol|li|div|h[1-6]|table|thead|tbody|tr|th|td|blockquote|pre)/i.test(html);

  while ((m = re.exec(raw))) {
    const before = raw.slice(last, m.index);
    if (before) out += marked.parse(before);

    const hid = (m[1] || "").trim();
    const inner = m[2] || "";
    const innerHtml = marked.parse(inner);
    const data = hid ? ` data-hid="${escapeHtml(hid)}"` : "";

    if (hasBlock(innerHtml)) {
      // block wrapper
      out += `<div class="passageTarget passageTarget--block"${data}>${innerHtml}</div>`;
    } else {
      // inline wrapper
      out += `<span class="passageTarget passageTarget--inline"${data}>${innerHtml}</span>`;
    }

    last = re.lastIndex;
  }

  const tail = raw.slice(last);
  if (tail) out += marked.parse(tail);
  return out;
}


export default function ReadingResultPage() {
  const { state } = useLocation();
  const navigate = useNavigate();

  const [attempt, setAttempt] = useState(null);
  const [readings, setReadings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [progress, setProgress] = useState(0);

  // ===== Robust highlight sync: ONE listener, multi-target, with inline fallback =====
  useEffect(() => {
    const HIGHLIGHT_BG = "rgba(255, 243, 205, 1)"; // soft amber
    const HIGHLIGHT_OUTLINE = "2px solid #ffd166";
    const esc =
      typeof window !== "undefined" && window.CSS && CSS.escape
        ? CSS.escape
        : (s) => String(s).replace(/"/g, '\\"');

    function applyHighlightInline(targets, on) {
      targets.forEach((t) => {
        if (on) {
          if (!t.dataset.prevStyle) {
            t.dataset.prevStyle = t.getAttribute("style") || "";
          }
          t.style.background = HIGHLIGHT_BG;
          t.style.outline = HIGHLIGHT_OUTLINE;
          t.style.borderRadius = "4px";
          t.classList.add("pulseOnce");
        } else {
          const prev = t.dataset.prevStyle || "";
          t.setAttribute("style", prev);
          delete t.dataset.prevStyle;
          t.classList.remove("pulseOnce");
        }

        // Make block children transparent so parent tint is visible
        t.querySelectorAll(
          ":scope > p, :scope > li, :scope > div, :scope > blockquote, :scope > pre, :scope > table"
        ).forEach((child) => {
          if (on) {
            if (!child.dataset.prevBg)
              child.dataset.prevBg = child.style.background || "";
            child.style.background = "transparent";
          } else {
            const prevBg = child.dataset.prevBg || "";
            child.style.background = prevBg;
            delete child.dataset.prevBg;
          }
        });
      });
    }

    function handleToggle(e) {
      const details =
        e.target instanceof HTMLElement &&
        e.target.matches("details.explainBlock")
          ? e.target
          : null;
      if (!details) return;

      const hid =
        details.getAttribute("data-hid") ||
        details.querySelector("summary.explainBtn")?.getAttribute("data-hid");
      if (!hid) return;

      const container = details.closest(`.${styles.resultContainer}`);
      if (!container) return;

      const leftPanel = container.querySelector(`.${styles.leftPanel}`);
      if (!leftPanel) return;

      const selector = `.passageTarget[data-hid="${esc(hid)}"]`;
      const targets = Array.from(leftPanel.querySelectorAll(selector));

      if (targets.length === 0) {
        console.warn("[Highlight] No passageTarget for id:", hid, "selector:", selector);
        return;
      }

      if (details.open) {
        targets.forEach((t) => t.classList.add("passageHighlight"));
        applyHighlightInline(targets, true);
        targets[0].scrollIntoView({ behavior: "smooth", block: "center" });
      } else {
        const anyOtherOpen = container.querySelectorAll(
          `details.explainBlock[open][data-hid="${esc(hid)}"]`
        ).length;
        if (!anyOtherOpen) {
          targets.forEach((t) => t.classList.remove("passageHighlight"));
          applyHighlightInline(targets, false);
        }
      }
    }

    document.addEventListener("toggle", handleToggle, true);
    return () => document.removeEventListener("toggle", handleToggle, true);
  }, []);

  if (!state) {
    return (
      <div className={styles.center}>
        <h2>No result data found</h2>
        <button onClick={() => navigate("/reading")} className={styles.backBtn}>
          ← Back
        </button>
      </div>
    );
  }

  const { attemptId, examName, isWaiting } = state;

  // ===== Fetch Attempt =====
  useEffect(() => {
    let timer;
    const fetchAttempt = async () => {
      try {
        const res = await ExamApi.getExamAttemptDetail(attemptId);
        if (res) {
          setAttempt(res);
          setLoading(false);
          clearInterval(timer);
        }
      } catch (err) {
        console.error("❌ Failed to fetch attempt:", err);
      }
    };

    if (isWaiting) {
      timer = setInterval(() => setProgress((p) => (p < 90 ? p + 5 : p)), 1000);
    }

    fetchAttempt();
    return () => clearInterval(timer);
  }, [attemptId, isWaiting]);

  // ===== Fetch Readings =====
  useEffect(() => {
    if (!attempt?.examId) return;
    (async () => {
      try {
        const res = await ReadingApi.getByExam(attempt.examId);
        setReadings(res || []);
      } catch (err) {
        console.error("Failed to load readings:", err);
      }
    })();
  }, [attempt]);

  if (loading) {
    return (
      <div className={styles.center}>
        <div className={styles.loadingSpinner}></div>
        <p className={styles.loadingText}>
          Evaluating your reading attempt... Please wait.
        </p>
        <div className={styles.progressBar}>
          <div
            className={styles.progressFill}
            style={{ width: `${progress}%` }}
          ></div>
        </div>
      </div>
    );
  }

  if (!attempt) {
    return (
      <div className={styles.center}>
        <h3>No attempt found.</h3>
        <button onClick={() => navigate("/reading")} className={styles.backBtn}>
          ← Back
        </button>
      </div>
    );
  }

  // ===== Parse user's answers =====
  const parsedAnswers = (() => {
    try {
      return JSON.parse(attempt.answerText || "[]");
    } catch {
      return [];
    }
  })();

  const userAnswerMap = new Map();
  parsedAnswers.forEach((g) => {
    if (g.SkillId && g.Answers) userAnswerMap.set(g.SkillId, g.Answers);
  });

  const attemptedSkillIds = [...userAnswerMap.keys()];

  // ===== Totals =====
  let totalQuestions = 0;
  let correctCount = 0;

  const normalize = (val) => {
    if (Array.isArray(val)) return val.map((x) => x.trim().toLowerCase());
    if (typeof val === "string") return [val.trim().toLowerCase()];
    return [];
  };

  const readingsWithCorrect = readings
    .filter((r) => attemptedSkillIds.includes(r.readingId))
    .map((r) => {
      let correctAnswers = {};
      try {
        correctAnswers = JSON.parse(r.correctAnswer || "{}");
      } catch {
        correctAnswers = {};
      }
      const userAnswers = userAnswerMap.get(r.readingId) || {};
      const correctKeys = Object.keys(correctAnswers);

      correctKeys.forEach((key) => {
        const userVal = normalize(userAnswers[key] || []);
        const correctVal = normalize(correctAnswers[key] || []);
        totalQuestions += correctVal.length;
        correctVal.forEach((opt) => {
          if (userVal.includes(opt.toLowerCase())) correctCount++;
        });
      });

      return { ...r, correctAnswers, userAnswers };
    });

  const accuracy =
    totalQuestions > 0 ? (correctCount / totalQuestions) * 100 : 0;

  return (
    <div className={styles.pageLayout}>
      <GeneralSidebar />
      <div className={styles.mainContent}>
        <div className={styles.header}>
          <h2>Reading Result — {examName || attempt.examName}</h2>
        </div>

        <div className={styles.bandScoreBox}>
          <div className={styles.bandOverall}>
            <h4>IELTS Band</h4>
            <div className={styles.bandMain}>
              {attempt.totalScore?.toFixed(1) || "-"}
            </div>
          </div>
          <div className={styles.bandGrid}>
            <div>
              <b>Correct</b>
              <p>{correctCount}</p>
            </div>
            <div>
              <b>Total</b>
              <p>{totalQuestions}</p>
            </div>
            <div>
              <b>Accuracy</b>
              <p>{accuracy.toFixed(1)}%</p>
            </div>
          </div>
        </div>

        {readingsWithCorrect.map((r, i) => (
          <div key={i} className={styles.resultContainer}>
            <div className={styles.leftPanel}>
              <h3>Passage {r.displayOrder || i + 1}</h3>
              <div
                className={styles.passageContent}
                dangerouslySetInnerHTML={{
                  __html:
                    passageWithAnchorsToHtml(r.readingContent) ||
                    "<i>No passage content</i>",
                }}
              />
            </div>

            <div className={styles.rightPanel}>
              <h3>Questions & Answers</h3>
              <ExamMarkdownRenderer
                markdown={r.readingQuestion}
                showAnswers={true}
                userAnswers={[{ SkillId: r.readingId, Answers: r.userAnswers }]}
                correctAnswers={[
                  { SkillId: r.readingId, Answers: r.correctAnswers },
                ]}
                skillId={r.readingId}
              />
            </div>
          </div>
        ))}

        <div className={styles.footer}>
          <button
            className={styles.backBtn}
            onClick={() => navigate("/reading")}
          >
            ← Back to Reading List
          </button>
        </div>
      </div>
    </div>
  );
}
