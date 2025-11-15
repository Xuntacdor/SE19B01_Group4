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
 */
function passageWithAnchorsToHtml(raw) {
  if (!raw) return "";

  // 1️⃣ Replace opening markers with inline spans
  const withOpen = raw.replace(
    /\[H(?:\*([^\]]*))?\]/g,
    (_match, id) => {
      const hid = (id || "").trim();
      const data = hid ? ` data-hid="${escapeHtml(hid)}"` : "";
      return `<span class="passageTarget passageTarget--inline"${data}>`;
    }
  );

  // 2️⃣ Replace closing markers (with or without id) with </span>
  const withClose = withOpen.replace(/\[\/H(?:\*([^\]]*))?\]/g, "</span>");

  // 3️⃣ Let marked handle paragraphs and line breaks for the whole thing
  return marked.parse(withClose);
}


export default function ReadingResultPage() {
  const { state } = useLocation();
  const navigate = useNavigate();

  const [attempt, setAttempt] = useState(null);
  const [readings, setReadings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [progress, setProgress] = useState(0);
  const [currentTask, setCurrentTask] = useState(0);

  // ===== Highlight Sync (we disable inline styles) =====
  useEffect(() => {
    const esc =
      typeof window !== "undefined" && window.CSS && CSS.escape
        ? CSS.escape
        : (s) => String(s).replace(/"/g, '\\"');

    // ⭐ FIXED: disabled fully
    function applyHighlightInline() {
      /* disabled */
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

      if (!targets.length) return;

      if (details.open) {
        targets.forEach((t) => t.classList.add("passageHighlight"));
        targets.forEach((t) => t.classList.add("pulseOnce"));

        targets[0].scrollIntoView({ behavior: "smooth", block: "center" });
      } else {
        const stillOpen = container.querySelectorAll(
          `details.explainBlock[open][data-hid="${esc(hid)}"]`
        ).length;

        if (!stillOpen) {
          targets.forEach((t) => t.classList.remove("passageHighlight"));
          targets.forEach((t) => t.classList.remove("pulseOnce"));
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
          setLoading(false);
        }
      } catch {
        setLoading(false);
      }
    };

    if (isWaiting) {
      timer = setInterval(
        () => setProgress((p) => (p < 90 ? p + 5 : p)),
        1000
      );
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
      } catch {
        setReadings([]);
      }
    })();
  }, [attempt]);

  // Ensure currentTask valid
  useEffect(() => {
    if (readings.length > 0 && currentTask >= readings.length) {
      setCurrentTask(0);
    }
  }, [readings.length, currentTask]);

  if (!state) {
    return (
      <div className={styles.center}>
        <h2>No result data found</h2>
        <button
          onClick={() => navigate("/reading")}
          className={styles.backBtn}
        >
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
        <button
          onClick={() => navigate("/reading")}
          className={styles.backBtn}
        >
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

  const isQuestionCorrect = (questionKey, userAnswers, correctAnswers) => {
    const userVal = normalize(userAnswers[questionKey] || []);
    const correctVal = normalize(correctAnswers[questionKey] || []);

    if (userVal.length === 0 || (userVal.length === 1 && userVal[0] === "_")) {
      return false;
    }

    if (correctVal.length === 0) return false;

    if (correctVal.length === 1) {
      const correctAnswer = correctVal[0];
      return userVal.some((u) => u === correctAnswer);
    }

    const correctSet = new Set(correctVal.filter((v) => v !== ""));
    const userSet = new Set(
      userVal.filter((v) => v !== "" && v !== "_")
    );

    if (correctSet.size !== userSet.size) return false;

    for (const c of correctSet) {
      if (!userSet.has(c)) return false;
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

      const questionStatus = {};
      correctKeys.forEach((key) => {
        const userVal = normalize(userAnswers[key] || []);
        const correctVal = normalize(correctAnswers[key] || []);
        totalQuestions += correctVal.length;

        const isCorrect = isQuestionCorrect(key, userAnswers, correctAnswers);
        const isAnswered = userVal.length > 0 && userVal[0] !== "_";

        questionStatus[key] = { isCorrect, isAnswered };

        correctVal.forEach((opt) => {
          if (userVal.includes(opt)) correctCount++;
        });
      });

      return { ...r, correctAnswers, userAnswers, questionStatus };
    });

  const accuracy =
    totalQuestions > 0 ? (correctCount / totalQuestions) * 100 : 0;

  const getQuestionCount = (readingQuestion) => {
    if (!readingQuestion) return 0;
    const markers = readingQuestion.match(/\[!num\]/g);
    return markers ? markers.length : 0;
  };

  const getQuestionStatus = (readingId, qNumber) => {
    const currentReading = readingsWithCorrect.find(
      (r) => r.readingId === readingId
    );
    if (!currentReading) return { isAnswered: false, isCorrect: false };

    const key = `${readingId}_q${qNumber}`;
    const status = currentReading.questionStatus[key];

    if (status) {
      const userAnswers = currentReading.userAnswers || {};
      const correctAnswers = currentReading.correctAnswers || {};
      const isCorrect = isQuestionCorrect(
        key,
        userAnswers,
        correctAnswers
      );
      return { isAnswered: status.isAnswered, isCorrect };
    }

    const userAnswers = currentReading.userAnswers || {};
    const correctAnswers = currentReading.correctAnswers || {};
    const userVal = normalize(userAnswers[key] || []);
    const isAnswered = userVal.length > 0 && userVal[0] !== "_";
    const isCorrect = isQuestionCorrect(key, userAnswers, correctAnswers);

    return { isAnswered, isCorrect };
  };

  const wrapperClass = examStyles?.examWrapper || "exam-wrapper";
  const topHeaderClass = examStyles?.topHeader || "top-header";
  const examTitleClass = examStyles?.examTitle || "exam-title";
  const mainContentClass = examStyles?.mainContent || "main-content";
  const leftPanelClass = examStyles?.leftPanel || "left-panel";
  const rightPanelClass = examStyles?.rightPanel || "right-panel";
  const bottomNavClass = examStyles?.bottomNavigation || "bottom-navigation";
  const completeBtnClass = examStyles?.completeButton || "complete-button";

  return (
    <div className={`${wrapperClass} ${styles.resultPageWrapper}`}>
      {/* Header */}
      <div className={`${topHeaderClass} ${styles.resultHeader}`}>
        <h2 className={`${examTitleClass} ${styles.resultTitle}`}>
          {examName || attempt?.examName || "Reading"} — Result
        </h2>
      </div>

      {/* Score Bar */}
      <div className={styles.scoreSummary}>
        <div className={`${styles.scoreItem} ${styles.scoreItemBand}`}>
          <b>Band Score</b>
          <span>{attempt?.totalScore?.toFixed(1) || "-"}</span>
        </div>

        <div className={styles.scoreItem}>
          <b>Correct</b>
          <span>{correctCount}</span>
        </div>

        <div className={styles.scoreItem}>
          <b>Total</b>
          <span>{totalQuestions}</span>
        </div>

        <div className={styles.scoreItem}>
          <b>Accuracy</b>
          <span>{accuracy.toFixed(1)}%</span>
        </div>
      </div>

      {/* Main Content */}
      {readingsWithCorrect.length > 0 && readingsWithCorrect[currentTask] ? (
        <div className={`${mainContentClass} ${styles.mainContent}`}>
          {/* Left: Passage */}
          <div className={`${leftPanelClass} ${styles.leftPanel}`}>
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
                  passageWithAnchorsToHtml(
                    readingsWithCorrect[currentTask]?.readingContent
                  ) || "<i>No passage content</i>",
              }}
            />
          </div>

          {/* Right: Questions */}
          <div className={`${rightPanelClass} ${styles.rightPanel}`}>
            <div className={examStyles.rightPanelDock}>
              <h3 className={styles.qaTitle}>
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
                  <div className={styles.emptyMessage}>
                    <h3>No Questions Found</h3>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      ) : (
        <div className={`${mainContentClass} ${styles.mainContent}`}>
          <div className={`${leftPanelClass} ${styles.leftPanel}`}>
            <div className={styles.emptyMessage}>
              <h3>No reading results available</h3>
            </div>
          </div>

          <div className={`${rightPanelClass} ${styles.rightPanel}`}>
            <div className={styles.emptyMessage}>
              <h3>No questions available</h3>
            </div>
          </div>
        </div>
      )}
      {/* Bottom Navigation */}
      <div className={`${bottomNavClass} ${styles.bottomNav}`}>
        <div className={styles.bottomNavList}>
          {readingsWithCorrect.length > 0 ? (
            readingsWithCorrect.map((task, taskIndex) => {
              const count = getQuestionCount(task.readingQuestion);
              const isActive = currentTask === taskIndex;

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
                  className={
                    isActive
                      ? `${styles.partCard} ${styles.partCardActive}`
                      : styles.partCard
                  }
                >
                  <div
                    className={
                      isActive
                        ? `${styles.partTitle} ${styles.partTitleActive}`
                        : styles.partTitle
                    }
                  >
                    Part {taskIndex + 1}
                  </div>

                  {isActive && count > 0 && (
                    <div className={styles.questionNumberList}>
                      {Array.from({ length: count }, (_, qIndex) => {
                        const qNum = qIndex + 1;
                        const status = getQuestionStatus(
                          task.readingId,
                          qNum
                        );
                        const isCorrect = status.isCorrect === true;
                        const isAnswered = status.isAnswered === true;
                        const isIncorrect = isAnswered && !isCorrect;
                        const isUnanswered = !isAnswered;

                        const shouldBeGreen = isCorrect && isAnswered;
                        const shouldBeRed = isIncorrect || isUnanswered;

                        let btnClass = styles.questionBtn;
                        if (shouldBeGreen) {
                          btnClass = `${styles.questionBtn} ${styles.questionBtnCorrect}`;
                        } else if (shouldBeRed) {
                          btnClass = `${styles.questionBtn} ${styles.questionBtnIncorrect}`;
                        }

                        const title = isUnanswered
                          ? "Not answered"
                          : isIncorrect
                          ? "Incorrect"
                          : isCorrect
                          ? "Correct"
                          : "Unknown";

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
                            className={btnClass}
                            title={title}
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
            <div className={styles.noPartsMessage}>No parts available</div>
          )}
        </div>

        <button
          className={`${completeBtnClass} ${styles.backToListButton}`}
          onClick={() => navigate("/reading")}
        >
          ← Back to Reading List
        </button>
      </div>
    </div>
  );
}
