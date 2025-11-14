// ListeningResultPage.jsx
import React, { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as ListeningApi from "../../Services/ListeningApi";
import * as ExamApi from "../../Services/ExamApi";
import { marked } from "marked";
import ExamMarkdownRenderer from "../../Components/Exam/ExamMarkdownRenderer";
import styles from "./ListeningResultPage.module.css";

// ---------- Markdown config for the TRANSCRIPT on the RESULT page ----------
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
 * Render the listening transcript like the reading passage:
 * - full Markdown support
 * - [H*id]...[/H] regions become .passageTarget anchors with data-hid
 * - inside [H] is also parsed as Markdown
 */
function transcriptWithAnchorsToHtml(raw) {
  if (!raw) return "";

  const re = /\[H(?:\*([^\]]*))?\]([\s\S]*?)\[\/H\]/g;
  let out = "";
  let last = 0;
  let m;

  const hasBlock = (html) =>
    /<(p|ul|ol|li|div|h[1-6]|table|thead|tbody|tr|th|td|blockquote|pre)/i.test(
      html
    );

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

export default function ListeningResultPage() {
  const { state } = useLocation();
  const navigate = useNavigate();

  const [attempt, setAttempt] = useState(null);
  const [listenings, setListenings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [progress, setProgress] = useState(0);

  // ===== Highlight sync: same behavior as ReadingResultPage =====
  useEffect(() => {
    const HIGHLIGHT_BG = "rgba(255, 243, 205, 1)";
    const HIGHLIGHT_OUTLINE = "2px solid #ffd166";

    const escFn =
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
            if (!child.dataset.prevBg) {
              child.dataset.prevBg = child.style.background || "";
            }
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

      const selector = `.passageTarget[data-hid="${escFn(hid)}"]`;
      const targets = Array.from(leftPanel.querySelectorAll(selector));

      if (targets.length === 0) {
        console.warn(
          "[Listening Highlight] No passageTarget for id:",
          hid,
          "selector:",
          selector
        );
        return;
      }

      if (details.open) {
        targets.forEach((t) => t.classList.add("passageHighlight"));
        applyHighlightInline(targets, true);
        targets[0].scrollIntoView({ behavior: "smooth", block: "center" });
      } else {
        const anyOtherOpen = container.querySelectorAll(
          `details.explainBlock[open][data-hid="${escFn(hid)}"]`
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
        <button
          onClick={() => navigate("/listening")}
          className={styles.backBtn}
        >
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
        console.error("❌ Failed to fetch attempt detail:", err);
        if (!isWaiting) {
          setLoading(false);
        }
      }
    };

    if (isWaiting) {
      timer = setInterval(
        () => setProgress((p) => (p < 90 ? p + 5 : p)),
        1000
      );
    }

    fetchAttempt();
    return () => clearInterval(timer);
  }, [attemptId, isWaiting]);

  // ===== Fetch Listening contents =====
  useEffect(() => {
    if (!attempt?.examId) return;
    const fetchListenings = async () => {
      try {
        const res = await ListeningApi.getByExam(attempt.examId);
        setListenings(res || []);
      } catch (err) {
        console.error("❌ Failed to fetch listening questions:", err);
      }
    };

    fetchListenings();
  }, [attempt?.examId]);

  if (loading) {
    return (
      <div className={styles.center}>
        <div className={styles.loadingCard}>
          <div className={styles.loadingIcon} />
          <h2>Processing your Listening result...</h2>
          {isWaiting ? (
            <>
              <p>Please wait a moment while we finalize your score.</p>
              <div className={styles.progressBar}>
                <div
                  className={styles.progressFill}
                  style={{ width: `${progress}%` }}
                />
              </div>
            </>
          ) : (
            <p>Loading your result...</p>
          )}
        </div>
      </div>
    );
  }

  if (!attempt) {
    return (
      <div className={styles.center}>
        <h2>Could not load listening attempt.</h2>
        <button
          onClick={() => navigate("/listening")}
          className={styles.backBtn}
        >
          ← Back
        </button>
      </div>
    );
  }

  // ===== Parse answers =====
  const parsedAnswers = (() => {
    try {
      return JSON.parse(attempt.answerText || "[]");
    } catch {
      return [];
    }
  })();

  const answerMap = new Map();
  parsedAnswers.forEach((g) => {
    if (g.SkillId && g.Answers) answerMap.set(g.SkillId, g.Answers);
  });

  const attemptedSkillIds = [...answerMap.keys()];

  // ===== Totals =====
  let totalQuestions = 0;
  let correctCount = 0;

  const normalize = (val) => {
    if (Array.isArray(val)) return val.map((x) => x.trim().toLowerCase());
    if (typeof val === "string") return [val.trim().toLowerCase()];
    return [];
  };

  // Only listenings that were actually attempted
  const relevantListenings = listenings.filter((l) =>
    attemptedSkillIds.includes(l.listeningId)
  );

  const listeningsWithCorrect = relevantListenings.map((l) => {
    let correctAnswers = {};
    try {
      correctAnswers = JSON.parse(l.correctAnswer || "{}");
    } catch {
      correctAnswers = {};
    }

    const userAnswers = answerMap.get(l.listeningId) || {};
    const correctKeys = Object.keys(correctAnswers);

    correctKeys.forEach((key) => {
      const userVal = normalize(userAnswers[key] || []);
      const correctVal = normalize(correctAnswers[key] || []);
      totalQuestions += correctVal.length;
      correctVal.forEach((opt) => {
        if (userVal.includes(opt.toLowerCase())) correctCount++;
      });
    });

    return { ...l, correctAnswers, userAnswers };
  });

  const accuracy =
    totalQuestions > 0 ? (correctCount / totalQuestions) * 100 : 0;

  const overallBand =
    typeof attempt.totalScore === "number"
      ? attempt.totalScore.toFixed(1)
      : "-";

  // ===== UI (mirror ReadingResultPage) =====
  return (
    <div className={styles.pageLayout}>
      <GeneralSidebar />
      <div className={styles.mainContent}>
        <div className={styles.header}>
          <h2>Listening Result — {examName || attempt.examName}</h2>
        </div>

        <div className={styles.bandScoreBox}>
          <div className={styles.bandOverall}>
            <h4>IELTS Band</h4>
            <div className={styles.bandMain}>{overallBand}</div>
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

        {listeningsWithCorrect.map((l, i) => {
          const userAnswers = l.userAnswers || {};
          const correctAnswers = l.correctAnswers || {};

          return (
            <div key={i} className={styles.resultContainer}>
              <div className={styles.leftPanel}>
                <h3>Section {l.displayOrder || i + 1}</h3>

                {l.listeningContent && (
                  <audio controls className={styles.audioPlayer}>
                    <source src={l.listeningContent} type="audio/mpeg" />
                    Your browser does not support the audio element.
                  </audio>
                )}

                <div
                  className={styles.passageContent}
                  dangerouslySetInnerHTML={{
                    __html:
                      l.transcript &&
                      transcriptWithAnchorsToHtml(l.transcript),
                  }}
                />
              </div>

              <div className={styles.rightPanel}>
                <h3>Questions & Answers</h3>
                <ExamMarkdownRenderer
                  markdown={l.listeningQuestion}
                  showAnswers={true}
                  userAnswers={[
                    { SkillId: l.listeningId, Answers: userAnswers },
                  ]}
                  correctAnswers={[
                    { SkillId: l.listeningId, Answers: correctAnswers },
                  ]}
                  skillId={l.listeningId}
                />
              </div>
            </div>
          );
        })}

        <div className={styles.footer}>
          <button
            className={styles.backBtn}
            onClick={() => navigate("/listening")}
          >
            ← Back to Listening List
          </button>
        </div>
      </div>
    </div>
  );
}
