// ListeningResultPage.jsx
import React, { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";

import * as ListeningApi from "../../Services/ListeningApi";
import * as ExamApi from "../../Services/ExamApi";

import { marked } from "marked";
import ExamMarkdownRenderer from "../../Components/Exam/ExamMarkdownRenderer";

// SAME LAYOUT AS READING
import examStyles from "./ListeningExamPage.module.css";

// LISTENING-SPECIFIC SCOREBAR + COLORS
import styles from "./ListeningResultPage.module.css";

// ---------- Markdown config for TRANSCRIPT ----------
marked.setOptions({
  gfm: true,
  breaks: true,
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
 * Transcript formatting with highlight anchors:
 * EXACT same logic as Reading (one-to-one clone)
 */
function transcriptWithAnchorsToHtml(raw) {
  if (!raw) return "";
  const withOpen = raw.replace(
    /\[H(?:\*([^\]]*))?\]/g,
    (_match, id) => {
      const hid = (id || "").trim();
      const data = hid ? ` data-hid="${escapeHtml(hid)}"` : "";
      return `<span class="passageTarget passageTarget--inline"${data}>`;
    }
  );
  const withClose = withOpen.replace(/\[\/H(?:\*([^\]]*))?\]/g, "</span>");
  return marked.parse(withClose);
}

export default function ListeningResultPage() {
  const { state } = useLocation();
  const navigate = useNavigate();

  const [attempt, setAttempt] = useState(null);
  const [listenings, setListenings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [progress, setProgress] = useState(0);
  const [currentPart, setCurrentPart] = useState(0);

  // ---------- Highlight Sync (same as Reading) ----------
  useEffect(() => {
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

      const selector = `.passageTarget[data-hid="${hid.replace(/"/g, '\\"')}"]`;
      const targets = Array.from(leftPanel.querySelectorAll(selector));

      if (targets.length === 0) return;

      if (details.open) {
        targets.forEach((t) => t.classList.add("passageHighlight"));
        targets.forEach((t) => t.classList.add("pulseOnce"));
        targets[0].scrollIntoView({ behavior: "smooth", block: "center" });
      } else {
        const stillOpen = container.querySelectorAll(
          `details.explainBlock[open][data-hid="${hid.replace(/"/g, '\\"')}"]`
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

  if (!state) {
    return (
      <div className={styles.center}>
        <h2>No listening result data found</h2>
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

  // ---------- Fetch attempt ----------
  useEffect(() => {
    let timer;
    async function loadAttempt() {
      try {
        const res = await ExamApi.getExamAttemptDetail(attemptId);
        if (res) {
          setAttempt(res);
          setLoading(false);
          clearInterval(timer);
        }
      } catch {
        if (!isWaiting) setLoading(false);
      }
    }

    if (isWaiting) {
      timer = setInterval(() => {
        setProgress((p) => (p < 90 ? p + 5 : p));
      }, 1000);
    }

    loadAttempt();
    return () => clearInterval(timer);
  }, [attemptId, isWaiting]);

  // ---------- Fetch listening passages ----------
  useEffect(() => {
    if (!attempt?.examId) return;

    (async () => {
      try {
        const res = await ListeningApi.getByExam(attempt.examId);
        setListenings(res || []);
      } catch {
        setListenings([]);
      }
    })();
  }, [attempt?.examId]);

  if (loading) {
    return (
      <div className={styles.center}>
        <div className={styles.loadingSpinner}></div>
        <p>Processing your listening result...</p>

        {isWaiting && (
          <div className={styles.progressBar}>
            <div
              className={styles.progressFill}
              style={{ width: `${progress}%` }}
            ></div>
          </div>
        )}
      </div>
    );
  }

  if (!attempt) {
    return (
      <div className={styles.center}>
        <h3>No attempt found.</h3>
        <button
          onClick={() => navigate("/listening")}
          className={styles.backBtn}
        >
          ← Back
        </button>
      </div>
    );
  }

  // ---------- Parse answers ----------
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

  // ---------------- TOTALS ----------------
  let totalQuestions = 0;
  let correctCount = 0;

  const normalize = (v) => {
    if (Array.isArray(v)) return v.map((x) => x.trim().toLowerCase());
    if (typeof v === "string") return [v.trim().toLowerCase()];
    return [];
  };

  const listeningsWithCorrect = listenings
    .filter((l) => attemptedSkillIds.includes(l.listeningId))
    .map((l) => {
      let correctAnswers = {};
      try {
        correctAnswers = JSON.parse(l.correctAnswer || "{}");
      } catch {
        correctAnswers = {};
      }

      const userAnswers = answerMap.get(l.listeningId) || {};
      const correctKeys = Object.keys(correctAnswers);

      correctKeys.forEach((qKey) => {
        const uVal = normalize(userAnswers[qKey] || []);
        const cVal = normalize(correctAnswers[qKey] || []);

        totalQuestions += cVal.length;
        cVal.forEach((opt) => {
          if (uVal.includes(opt)) correctCount++;
        });
      });

      return { ...l, correctAnswers, userAnswers };
    });

  const accuracy =
    totalQuestions > 0 ? (correctCount / totalQuestions) * 100 : 0;

  // Count questions from markers
  const getQuestionCount = (markdown) => {
    if (!markdown) return 0;
    const matches = markdown.match(/\[!num\]/g);
    return matches ? matches.length : 0;
  };

  const overallBand =
    typeof attempt.totalScore === "number"
      ? attempt.totalScore.toFixed(1)
      : "-";

  // ===========================================
  //              MAIN RENDER
  // ===========================================
  return (
    <div className={`${examStyles.examWrapper} ${styles.resultPageWrapper}`}>
      {/* Header */}
      <div className={`${examStyles.topHeader} ${styles.resultHeader}`}>
        <h2 className={`${examStyles.examTitle} ${styles.resultTitle}`}>
          Listening Result — {examName || attempt.examName}
        </h2>
      </div>

      {/* Score Summary */}
      <div className={styles.scoreSummary}>
        <div className={`${styles.scoreItem} ${styles.scoreItemBand}`}>
          <b>Band Score</b>
          <span>{overallBand}</span>
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
      {listeningsWithCorrect.length > 0 &&
      listeningsWithCorrect[currentPart] ? (
        <div className={`${examStyles.mainContent} ${styles.mainContent}`}>
          {/* LEFT PANEL */}
          <div className={`${examStyles.leftPanel} ${styles.leftPanel}`}>
            <h3 className={examStyles.passageTitle}>
              Section {listeningsWithCorrect[currentPart].displayOrder ||
                currentPart + 1}
            </h3>

            {/* Audio */}
            {listeningsWithCorrect[currentPart].listeningContent ? (
              <audio
                controls
                className={styles.audioPlayer}
              >
                <source
                  src={listeningsWithCorrect[currentPart].listeningContent}
                  type="audio/mpeg"
                />
                Your browser does not support the audio element.
              </audio>
            ) : (
              <p className={styles.noAudio}>No audio provided</p>
            )}

            {/* Transcript */}
            <div
              className={examStyles.passageContent}
              dangerouslySetInnerHTML={{
                __html: transcriptWithAnchorsToHtml(
                  listeningsWithCorrect[currentPart].transcript
                ),
              }}
            />
          </div>

          {/* RIGHT PANEL */}
          <div className={`${examStyles.rightPanel} ${styles.rightPanel}`}>
            <div className={examStyles.rightPanelDock}>
              <h3 className={styles.qaTitle}>
                Questions & Answers — Part {currentPart + 1}
              </h3>

              <ExamMarkdownRenderer
                markdown={listeningsWithCorrect[currentPart].listeningQuestion}
                showAnswers={true}
                userAnswers={[
                  {
                    SkillId:
                      listeningsWithCorrect[currentPart].listeningId,
                    Answers:
                      listeningsWithCorrect[currentPart].userAnswers,
                  },
                ]}
                correctAnswers={[
                  {
                    SkillId:
                      listeningsWithCorrect[currentPart].listeningId,
                    Answers:
                      listeningsWithCorrect[currentPart].correctAnswers,
                  },
                ]}
                skillId={listeningsWithCorrect[currentPart].listeningId}
              />
            </div>
          </div>
        </div>
      ) : (
        <div className={`${examStyles.mainContent} ${styles.mainContent}`}>
          <div className={`${examStyles.leftPanel} ${styles.leftPanel}`}>
            <h3>No listening content found</h3>
          </div>

          <div className={`${examStyles.rightPanel} ${styles.rightPanel}`}>
            <h3>No questions available</h3>
          </div>
        </div>
      )}

      {/* Bottom Navigation */}
      <div className={`${examStyles.bottomNavigation} ${styles.bottomNav}`}>
        <div className={styles.bottomNavList}>
          {listeningsWithCorrect.map((part, index) => {
            const count = getQuestionCount(part.listeningQuestion);
            const isActive = currentPart === index;

            return (
              <div
                key={part.listeningId}
                role="button"
                onClick={() => {
                  setCurrentPart(index);
                  document
                    .querySelector(`.${examStyles.examWrapper}`)
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
                  Part {index + 1}
                </div>

                {isActive && count > 0 && (
                  <div className={styles.questionNumberList}>
                    {Array.from({ length: count }, (_, qIndex) => {
                      const qNum = qIndex + 1;

                      const key = `${part.listeningId}_q${qNum}`;
                      const userVal = normalize(
                        part.userAnswers?.[key] || []
                      );
                      const isAnswered =
                        userVal.length > 0 && userVal[0] !== "_";

                      const correctVal = normalize(
                        part.correctAnswers?.[key] || []
                      );

                      const isCorrect =
                        isAnswered &&
                        correctVal.some((c) => userVal.includes(c));

                      const isIncorrect =
                        isAnswered && !isCorrect;

                      const isUnanswered = !isAnswered;

                      let className = styles.questionBtn;
                      if (isCorrect)
                        className += ` ${styles.questionBtnCorrect}`;
                      else if (isIncorrect || isUnanswered)
                        className += ` ${styles.questionBtnIncorrect}`;

                      return (
                        <button
                          type="button"
                          key={`${index}-${qIndex}`}
                          className={className}
                          title={
                            isUnanswered
                              ? "Not answered"
                              : isIncorrect
                              ? "Incorrect"
                              : "Correct"
                          }
                          onClick={(e) => {
                            e.stopPropagation();
                            setCurrentPart(index);
                            document
                              .querySelector(`.${examStyles.examWrapper}`)
                              ?.scrollTo({ top: 0, behavior: "smooth" });
                          }}
                        >
                          {qNum}
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}
        </div>

        <button
          className={`${examStyles.completeButton} ${styles.backToListButton}`}
          onClick={() => navigate("/listening")}
        >
          ← Back to Listening List
        </button>
      </div>
    </div>
  );
}
