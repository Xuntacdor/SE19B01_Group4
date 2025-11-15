// ReadingResultPage.jsx
import React, { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import * as ReadingApi from "../../Services/ReadingApi";
import * as ExamApi from "../../Services/ExamApi";
import { marked } from "marked";
import styles from "./ReadingResultPage.module.css";
import examStyles from "./ReadingExamPage.module.css";
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
  const [currentTask, setCurrentTask] = useState(0);

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

      const container = details.closest(`.${examStyles.examWrapper}`);
      if (!container) return;

      const leftPanel = container.querySelector(`.${examStyles.leftPanel}`);
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

  // Extract state values safely
  const attemptId = state?.attemptId;
  const examName = state?.examName;
  const isWaiting = state?.isWaiting;

  // ===== Fetch Attempt =====
  useEffect(() => {
    if (!attemptId) return;
    
    let timer;
    const fetchAttempt = async () => {
      try {
        const res = await ExamApi.getExamAttemptDetail(attemptId);
        if (res) {
          setAttempt(res);
          setLoading(false);
          clearInterval(timer);
        } else {
          console.warn("No attempt data received");
          setLoading(false);
        }
      } catch (err) {
        console.error("❌ Failed to fetch attempt:", err);
        setLoading(false);
      }
    };

    if (isWaiting) {
      timer = setInterval(() => setProgress((p) => (p < 90 ? p + 5 : p)), 1000);
    } else {
      setLoading(false);
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
        setReadings([]);
      }
    })();
  }, [attempt]);

  // Ensure currentTask doesn't exceed available readings
  useEffect(() => {
    if (readings.length > 0 && currentTask >= readings.length) {
      setCurrentTask(0);
    }
  }, [readings.length, currentTask]);

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

  // Helper function to check if a question is correct
  const isQuestionCorrect = (questionKey, userAnswers, correctAnswers) => {
    const userVal = normalize(userAnswers[questionKey] || []);
    const correctVal = normalize(correctAnswers[questionKey] || []);
    
    // Not answered
    if (userVal.length === 0 || (userVal.length === 1 && userVal[0] === "_")) {
      return false;
    }
    
    // Empty correct answer means no correct answer expected
    if (correctVal.length === 0) {
      return false;
    }
    
    // For single answer questions, check if user answer matches any correct answer
    if (correctVal.length === 1) {
      const correctAnswer = correctVal[0].toLowerCase().trim();
      return userVal.some(userAns => userAns.toLowerCase().trim() === correctAnswer);
    }
    
    // For multiple choice, check if user selected ALL correct options AND no extra options
    // First, remove duplicates and normalize
    const correctSet = new Set(correctVal.map(v => v.toLowerCase().trim()).filter(v => v !== ""));
    const userSet = new Set(userVal.map(v => v.toLowerCase().trim()).filter(v => v !== "" && v !== "_"));
    
    // Must have same number of answers
    if (correctSet.size !== userSet.size) {
      return false;
    }
    
    // All correct answers must be in user answers
    for (const correct of correctSet) {
      if (!userSet.has(correct)) {
        return false;
      }
    }
    
    // All user answers must be correct (no extra wrong answers)
    for (const user of userSet) {
      if (!correctSet.has(user)) {
        return false;
      }
    }
    
    return true;
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

      // Create a map to track which questions are correct/incorrect
      const questionStatus = {};
      correctKeys.forEach((key) => {
        const userVal = normalize(userAnswers[key] || []);
        const correctVal = normalize(correctAnswers[key] || []);
        totalQuestions += correctVal.length;
        
        const isCorrect = isQuestionCorrect(key, userAnswers, correctAnswers);
        const isAnswered = userVal.length > 0;
        questionStatus[key] = {
          isCorrect,
          isAnswered
        };
        
        correctVal.forEach((opt) => {
          if (userVal.includes(opt.toLowerCase())) correctCount++;
        });
      });

      return { ...r, correctAnswers, userAnswers, questionStatus };
    });

  const accuracy =
    totalQuestions > 0 ? (correctCount / totalQuestions) * 100 : 0;

  // For nav buttons: count [!num]
  const getQuestionCount = (readingQuestion) => {
    if (!readingQuestion) return 0;
    const numMarkers = readingQuestion.match(/\[!num\]/g);
    return numMarkers ? numMarkers.length : 0;
  };

  // Helper to check if question is correct/incorrect for styling
  const getQuestionStatus = (readingId, qNumber) => {
    const currentReading = readingsWithCorrect.find(r => r.readingId === readingId);
    if (!currentReading) return { isAnswered: false, isCorrect: false };
    
    const questionKey = `${readingId}_q${qNumber}`;
    const status = currentReading.questionStatus[questionKey];
    
    // If status exists, use it
    if (status) {
      // Double-check correctness for accuracy
      const userAnswers = currentReading.userAnswers || {};
      const correctAnswers = currentReading.correctAnswers || {};
      const isActuallyCorrect = isQuestionCorrect(questionKey, userAnswers, correctAnswers);
      
      return {
        isAnswered: status.isAnswered,
        isCorrect: isActuallyCorrect // Use re-calculated value for accuracy
      };
    }
    
    // If no status found, check manually
    const userAnswers = currentReading.userAnswers || {};
    const correctAnswers = currentReading.correctAnswers || {};
    const userVal = normalize(userAnswers[questionKey] || []);
    const isAnswered = userVal.length > 0 && !(userVal.length === 1 && userVal[0] === "_");
    const isCorrect = isQuestionCorrect(questionKey, userAnswers, correctAnswers);
    
    return { isAnswered, isCorrect };
  };

  // Fallback if CSS modules aren't loading
  const wrapperClass = examStyles?.examWrapper || "exam-wrapper";
  const topHeaderClass = examStyles?.topHeader || "top-header";
  const examTitleClass = examStyles?.examTitle || "exam-title";
  const mainContentClass = examStyles?.mainContent || "main-content";
  const leftPanelClass = examStyles?.leftPanel || "left-panel";
  const rightPanelClass = examStyles?.rightPanel || "right-panel";
  const bottomNavClass = examStyles?.bottomNavigation || "bottom-navigation";
  const completeBtnClass = examStyles?.completeButton || "complete-button";

  return (
    <div className={wrapperClass} style={{ minHeight: "100vh", display: "flex", flexDirection: "column" }}>
      {/* Header */}
      <div className={topHeaderClass} style={{ display: "flex", justifyContent: "center", alignItems: "center", padding: "16px 24px", borderBottom: "1px solid #e0e0e0" }}>
        <h2 className={examTitleClass} style={{ fontSize: "20px", fontWeight: "bold", margin: 0 }}>
          {examName || attempt?.examName || "Reading"} — Result
        </h2>
      </div>

      {/* Score Summary */}
      <div className={styles.scoreSummary} style={{ display: "flex", gap: "16px", padding: "16px 24px", background: "#f9fafb", borderBottom: "1px solid #e5e7eb", justifyContent: "space-between", width: "100%" }}>
        <div className={styles.scoreItem} style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: "6px", padding: "16px", background: "linear-gradient(135deg, #22c55e 0%, #16a34a 100%)", border: "none", borderRadius: "12px", flex: "1", minWidth: "0", boxShadow: "0 2px 8px rgba(34, 197, 94, 0.2)" }}>
          <b style={{ fontSize: "12px", color: "#ffffff", textTransform: "uppercase", letterSpacing: "0.5px", opacity: 0.9, fontWeight: 600 }}>Band Score</b>
          <span style={{ fontSize: "32px", fontWeight: 700, color: "#ffffff", lineHeight: 1 }}>{attempt?.totalScore?.toFixed(1) || "-"}</span>
        </div>
        <div className={styles.scoreItem} style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: "6px", padding: "16px", background: "#ffffff", border: "1px solid #e5e7eb", borderRadius: "12px", flex: "1", minWidth: "0", boxShadow: "0 1px 3px rgba(0, 0, 0, 0.1)" }}>
          <b style={{ fontSize: "12px", color: "#6b7280", textTransform: "uppercase", letterSpacing: "0.5px", fontWeight: 600 }}>Correct</b>
          <span style={{ fontSize: "28px", fontWeight: 700, color: "#111827" }}>{correctCount}</span>
        </div>
        <div className={styles.scoreItem} style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: "6px", padding: "16px", background: "#ffffff", border: "1px solid #e5e7eb", borderRadius: "12px", flex: "1", minWidth: "0", boxShadow: "0 1px 3px rgba(0, 0, 0, 0.1)" }}>
          <b style={{ fontSize: "12px", color: "#6b7280", textTransform: "uppercase", letterSpacing: "0.5px", fontWeight: 600 }}>Total</b>
          <span style={{ fontSize: "28px", fontWeight: 700, color: "#111827" }}>{totalQuestions}</span>
        </div>
        <div className={styles.scoreItem} style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: "6px", padding: "16px", background: "#ffffff", border: "1px solid #e5e7eb", borderRadius: "12px", flex: "1", minWidth: "0", boxShadow: "0 1px 3px rgba(0, 0, 0, 0.1)" }}>
          <b style={{ fontSize: "12px", color: "#6b7280", textTransform: "uppercase", letterSpacing: "0.5px", fontWeight: 600 }}>Accuracy</b>
          <span style={{ fontSize: "28px", fontWeight: 700, color: "#111827" }}>{accuracy.toFixed(1)}%</span>
        </div>
      </div>

      {/* Main Content */}
      {readingsWithCorrect.length > 0 && readingsWithCorrect[currentTask] ? (
        <div className={mainContentClass} style={{ display: "flex", flex: 1, overflow: "hidden" }}>
          {/* Left: Passage */}
          <div className={leftPanelClass} style={{ flex: 1, padding: "24px", overflowY: "auto", borderRight: "1px solid #e0e0e0" }}>
            {readingsWithCorrect[currentTask]?.passageTitle && (
              <div className={examStyles.passageHeader}>
                <h3 className={examStyles.passageTitle}>
                  {readingsWithCorrect[currentTask].passageTitle}
                </h3>
              </div>
            )}
            <div
              className={examStyles.passageContent}
              dangerouslySetInnerHTML={{
                __html:
                  passageWithAnchorsToHtml(readingsWithCorrect[currentTask]?.readingContent) ||
                  "<i>No passage content</i>",
              }}
            />
          </div>

          {/* Right: Questions & Answers */}
          <div className={rightPanelClass} style={{ flex: 1, padding: "24px", overflowY: "auto" }}>
            <div className={examStyles.rightPanelDock}>
              <h3 style={{ marginTop: 0, marginBottom: 16 }}>
                Questions & Answers — Part {currentTask + 1}
              </h3>
              {readingsWithCorrect[currentTask]?.readingQuestion ? (
                <ExamMarkdownRenderer
                  markdown={readingsWithCorrect[currentTask].readingQuestion}
                  showAnswers={true}
                  userAnswers={[
                    {
                      SkillId: readingsWithCorrect[currentTask].readingId,
                      Answers: readingsWithCorrect[currentTask].userAnswers,
                    },
                  ]}
                  correctAnswers={[
                    {
                      SkillId: readingsWithCorrect[currentTask].readingId,
                      Answers: readingsWithCorrect[currentTask].correctAnswers,
                    },
                  ]}
                  skillId={readingsWithCorrect[currentTask].readingId}
                />
              ) : (
                <div className={examStyles.questionSection}>
                  <div
                    style={{
                      padding: "40px",
                      textAlign: "center",
                      color: "#666",
                    }}
                  >
                    <h3>No Questions Found</h3>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      ) : (
        <div className={mainContentClass} style={{ display: "flex", flex: 1, overflow: "hidden" }}>
          <div className={leftPanelClass} style={{ flex: 1, padding: "24px", overflowY: "auto", borderRight: "1px solid #e0e0e0" }}>
            <div style={{ padding: "40px", textAlign: "center", color: "#666" }}>
              <h3>No reading results available</h3>
              <p>There are no reading tasks to display.</p>
            </div>
          </div>
          <div className={rightPanelClass} style={{ flex: 1, padding: "24px", overflowY: "auto" }}>
            <div style={{ width: "100%" }}>
              <div style={{ padding: "40px", textAlign: "center", color: "#666" }}>
                <h3>No questions available</h3>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Bottom Navigation */}
      <div className={bottomNavClass} style={{ display: "flex", alignItems: "center", justifyContent: "flex-start", padding: "12px 20px", borderTop: "1px solid #ddd", background: "#fafafa", gap: "12px" }}>
        <div style={{ display: "flex", overflowX: "auto", gap: "12px", flex: "0 1 auto", padding: "2px 0", scrollbarWidth: "thin" }}>
          {readingsWithCorrect.length > 0 ? (
            readingsWithCorrect.map((task, taskIndex) => {
              const count = getQuestionCount(task.readingQuestion);
              return (
                <div
                  key={task.readingId}
                  role="button"
                  onClick={() => {
                    setCurrentTask(taskIndex);
                    document
                      .querySelector(`.${wrapperClass}`)
                      ?.scrollTo({ top: 0, behavior: "smooth" });
                  }}
                  style={{ 
                    display: "flex", 
                    flexDirection: "column", 
                    alignItems: "center", 
                    padding: "8px 12px", 
                    background: "#ffffff", 
                    border: `1px solid ${currentTask === taskIndex ? "#2563eb" : "#e5e7eb"}`, 
                    borderRadius: "6px", 
                    cursor: "pointer",
                    minWidth: "140px",
                    flexShrink: 0,
                    boxShadow: currentTask === taskIndex ? "0 2px 6px rgba(0, 0, 0, 0.15)" : "0 1px 2px rgba(0, 0, 0, 0.08)",
                    transition: "all 0.2s ease"
                  }}
                  onMouseEnter={(e) => {
                    if (currentTask !== taskIndex) {
                      e.currentTarget.style.boxShadow = "0 2px 4px rgba(0, 0, 0, 0.12)";
                      e.currentTarget.style.borderColor = "#cbd5e1";
                    }
                  }}
                  onMouseLeave={(e) => {
                    if (currentTask !== taskIndex) {
                      e.currentTarget.style.boxShadow = "0 1px 2px rgba(0, 0, 0, 0.08)";
                      e.currentTarget.style.borderColor = "#e5e7eb";
                    }
                  }}
                >
                  <div
                    style={{
                      fontWeight: currentTask === taskIndex ? 700 : 600,
                      fontSize: "12px",
                      color: currentTask === taskIndex ? "#2563eb" : "#374151",
                      padding: "2px 6px",
                      borderRadius: "4px",
                      width: "100%",
                      textAlign: "center",
                      background: currentTask === taskIndex ? "#eff6ff" : "transparent"
                    }}
                  >
                    Part {taskIndex + 1}
                  </div>
                  {currentTask === taskIndex && count > 0 && (
                    <div style={{ display: "flex", gap: "4px", flexWrap: "wrap", justifyContent: "center", width: "100%", marginTop: "6px" }}>
                      {Array.from({ length: count }, (_, qIndex) => {
                        const qNum = qIndex + 1;
                        const status = getQuestionStatus(task.readingId, qNum);
                        const isCorrect = status.isCorrect === true; // Explicitly check for true
                        const isAnswered = status.isAnswered === true;
                        const isIncorrect = isAnswered && !isCorrect; // Only incorrect if answered but wrong
                        const isUnanswered = !isAnswered;
                        
                        // Determine styling: Green for correct, Red for incorrect/unanswered
                        const shouldBeGreen = isCorrect && isAnswered;
                        const shouldBeRed = isIncorrect || isUnanswered;
                        
                        return (
                          <button
                            type="button"
                            key={`${taskIndex}-${qIndex}`}
                            onClick={(e) => {
                              e.stopPropagation();
                              setCurrentTask(taskIndex);
                              document
                                .querySelector(`.${wrapperClass}`)
                                ?.scrollTo({ top: 0, behavior: "smooth" });
                            }}
                            style={{
                              border: shouldBeRed
                                ? "1px solid #ef4444" 
                                : shouldBeGreen
                                ? "1px solid #22c55e" 
                                : "1px solid #ccc",
                              background: shouldBeRed
                                ? "#fee2e2" 
                                : shouldBeGreen
                                ? "#ecfdf5" 
                                : "white",
                              color: shouldBeRed
                                ? "#dc2626" 
                                : shouldBeGreen
                                ? "#166534" 
                                : "#333",
                              padding: "4px 8px",
                              borderRadius: "4px",
                              fontSize: "13px",
                              cursor: "pointer",
                              minWidth: "32px",
                              fontWeight: shouldBeRed ? 700 : shouldBeGreen ? 600 : 400,
                              transition: "all 0.2s ease"
                            }}
                            title={isUnanswered ? "Not answered" : isIncorrect ? "Incorrect" : isCorrect ? "Correct" : "Unknown"}
                          >
                            {qNum}
                          </button>
                        );
                      })}
                    </div>
                  )}
                </div>
              );
            })
          ) : (
            <div style={{ padding: "8px 16px", color: "#666" }}>
              No parts available
            </div>
          )}
        </div>

        <button
          className={completeBtnClass}
          onClick={() => navigate("/reading")}
          style={{ 
            background: "#16a34a", 
            color: "white", 
            fontWeight: 600, 
            border: "none", 
            borderRadius: "6px", 
            padding: "8px 14px", 
            cursor: "pointer", 
            marginLeft: "auto" 
          }}
        >
          ← Back to Reading List
        </button>
      </div>
    </div>
  );
}
