import React, { useEffect, useState, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { marked } from "marked";
import { submitReadingAttempt } from "../../Services/ReadingApi";
import ExamMarkdownRenderer from "../../Components/Exam/ExamMarkdownRenderer";
import { Highlighter, Trash2, Pencil } from "lucide-react";
import ConfirmationPopup from "../../Components/Common/ConfirmationPopup";
import styles from "./ReadingExamPage.module.css";

// ---------- Markdown config for the PASSAGE ----------
marked.setOptions({
  gfm: true,
  breaks: true,
  mangle: false,
  headerIds: false,
});

// Remove [H*id] wrappers
function passageMarkdownToHtml(raw) {
  if (!raw) return "";
  const cleaned = String(raw).replace(
    /\[H(?:\*([^\]]*))?\]([\s\S]*?)\[\/H\]/g,
    (_match, _id, inner) => inner
  );
  return marked.parse(cleaned);
}

// ----- DOM serialization helpers omitted (unchanged) -----

export default function ReadingExamPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const { exam, tasks, duration } = state || {};

  const [currentTask, setCurrentTask] = useState(0);
  const [submitted, setSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [timeLeft, setTimeLeft] = useState(duration ? duration * 60 : 0);
  const [answers, setAnswers] = useState({});
  const formRef = useRef(null);

  const [highlightMode, setHighlightMode] = useState(false);
  const [highlights, setHighlights] = useState([]);
  const [contextMenu, setContextMenu] = useState(null);
  const [pendingSelection, setPendingSelection] = useState(null);
  const passageContentRef = useRef(null);

  // NEW: Popup
  const [popup, setPopup] = useState({ open: false, message: "" });

  // Countdown timer
  useEffect(() => {
    if (!timeLeft || submitted) return;
    const timer = setInterval(() => {
      setTimeLeft((t) => Math.max(0, t - 1));
    }, 1000);
    return () => clearInterval(timer);
  }, [timeLeft, submitted]);

  const formatTime = (sec) => {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${m}:${s < 10 ? "0" + s : s}`;
  };

  // Track answers
  const handleChange = (e) => {
    const { name, value, type, checked, multiple, options, dataset } = e.target;
    if (!name) return;

    if (type === "checkbox") {
      const limit = parseInt(dataset.limit || "0", 10);
      const group = formRef.current?.querySelectorAll(
        `input[name="${name}"][type="checkbox"]`
      );
      const checkedInGroup = Array.from(group || []).filter((el) => el.checked);

      if (checked && limit && checkedInGroup.length > limit) {
        e.preventDefault();
        e.target.checked = false;

        setPopup({
          open: true,
          message: `You can only select ${limit} option${limit > 1 ? "s" : ""}.`,
        });

        return;
      }

      const selected = checkedInGroup.map((el) => el.value);
      setAnswers((prev) => ({ ...prev, [name]: selected }));
      return;
    }

    if (type === "radio") {
      if (checked) setAnswers((prev) => ({ ...prev, [name]: value }));
      return;
    }

    if (multiple) {
      const selected = Array.from(options)
        .filter((opt) => opt.selected)
        .map((opt) => opt.value);
      setAnswers((prev) => ({ ...prev, [name]: selected }));
      return;
    }

    setAnswers((prev) => ({ ...prev, [name]: value }));
  };

  // Submit attempt
  const handleSubmit = (e) => {
    e?.preventDefault();
    if (isSubmitting) return;

    const structuredAnswers = (tasks || []).map((task) => {
      const prefix = `${task.readingId}_q`;
      const questionKeys = Object.keys(answers)
        .filter((k) => k.startsWith(prefix))
        .sort((a, b) => {
          const na = parseInt(a.split("_q")[1]) || 0;
          const nb = parseInt(b.split("_q")[1]) || 0;
          return na - nb;
        });

      const taskAnswers = {};
      questionKeys.forEach((key) => {
        const val = answers[key];
        if (Array.isArray(val) && val.length > 0) taskAnswers[key] = val;
        else if (typeof val === "string" && val.trim() !== "")
          taskAnswers[key] = val;
        else taskAnswers[key] = "_";
      });

      return { SkillId: task.readingId, Answers: taskAnswers };
    });

    setIsSubmitting(true);
    const jsonString = JSON.stringify(structuredAnswers);
    const attempt = {
      examId: exam.examId,
      startedAt: new Date().toISOString(),
      answers: jsonString,
    };

    submitReadingAttempt(attempt)
      .then((res) => {
        const attempt = res.data;
        navigate("/reading/result", {
          state: {
            attemptId: attempt.attemptId,
            examName: attempt.examName,
            isWaiting: true,
          },
        });
        setSubmitted(true);
      })
      .catch(() => {
        setPopup({
          open: true,
          message: `Failed to submit your reading attempt.`,
        });
      })
      .finally(() => setIsSubmitting(false));
  };

  const getQuestionCount = (readingQuestion) => {
    if (!readingQuestion) return 0;
    const numMarkers = readingQuestion.match(/\[!num\]/g);
    return numMarkers ? numMarkers.length : 0;
  };

  const toggleHighlightMode = () => {
    setHighlightMode((prev) => !prev);
    setContextMenu(null);
    setPendingSelection(null);
    const sel = window.getSelection();
    if (sel) sel.removeAllRanges();
  };

  // Selection, highlight, context menu logic (unchanged) …

  if (!exam)
    return (
      <div className={styles.fullscreenCenter}>
        <h2>No exam selected</h2>
        <button className={styles.backBtn} onClick={() => navigate("/reading")}>
          ← Back
        </button>
      </div>
    );

  if (submitted)
    return (
      <div className={styles.fullscreenCenter}>
        <h3>✅ Reading Test Submitted!</h3>
        <p>Your answers have been recorded successfully.</p>
        <button className={styles.backBtn} onClick={() => navigate("/reading")}>
          ← Back to Reading List
        </button>
      </div>
    );

  const currentTaskData = (tasks || [])[currentTask];
  const questionCount = getQuestionCount(currentTaskData?.readingQuestion);

  const isAnswered = (readingId, qNumber) => {
    const key = `${readingId}_q${qNumber}`;
    const val = answers[key];
    if (Array.isArray(val)) return val.length > 0;
    if (typeof val === "string") return val.trim() !== "";
    return false;
  };

  return (
    <div className={styles.examWrapper}>
      {/* Header */}
      <div className={styles.topHeader}>
        <button className={styles.backBtn} onClick={() => navigate("/reading")}>
          ← Back
        </button>
        <h2 className={styles.examTitle}>{exam.examName}</h2>
        <div className={styles.timer}>⏰ {formatTime(timeLeft)}</div>
      </div>

      <div className={styles.mainContent}>
        {/* Passage */}
        <div className={styles.leftPanel}>
          {currentTaskData?.passageTitle && (
            <div className={styles.passageHeader}>
              <h3 className={styles.passageTitle}>
                {currentTaskData.passageTitle}
              </h3>
            </div>
          )}

          <div
            ref={passageContentRef}
            className={`${styles.passageContent} ${
              highlightMode ? styles.highlightMode : ""
            }`}
            dangerouslySetInnerHTML={{
              __html: passageMarkdownToHtml(
                currentTaskData?.readingContent || ""
              ),
            }}
          />
        </div>

        {/* Questions */}
        <div className={styles.rightPanel}>
          <div className={styles.rightPanelDock}>
            <form ref={formRef} onChange={handleChange} onInput={handleChange}>
              {currentTaskData?.readingQuestion ? (
                <ExamMarkdownRenderer
                  markdown={currentTaskData.readingQuestion}
                  showAnswers={false}
                  skillId={currentTaskData.readingId}
                />
              ) : (
                <div className={styles.questionSection}>
                  <div
                    style={{
                      padding: "40px",
                      textAlign: "center",
                      color: "#666",
                    }}
                  >
                    <h3>No Questions Found</h3>
                    <p>This reading test doesn't have any questions configured yet.</p>
                  </div>
                </div>
              )}
            </form>
          </div>
        </div>
      </div>

      {/* INSERTED POPUP */}
      <ConfirmationPopup
        isOpen={popup.open}
        title="Notice"
        message={popup.message}
        onClose={() => setPopup({ open: false, message: "" })}
        onConfirm={() => setPopup({ open: false, message: "" })}
        confirmText="OK"
        cancelText="Close"
      />

      {/* Bottom Navigation */}
      <div className={styles.bottomNavigation}>
        <div className={styles.navScrollContainer}>
          {(tasks || []).map((task, taskIndex) => {
            const count = getQuestionCount(task.readingQuestion);
            return (
              <div
                key={task.readingId}
                className={styles.navSection}
                onClick={() => {
                  setCurrentTask(taskIndex);
                  document
                    .querySelector(`.${styles.examWrapper}`)
                    ?.scrollTo({ top: 0, behavior: "smooth" });
                }}
                role="button"
              >
                <div
                  className={`${styles.navSectionTitle} ${
                    currentTask === taskIndex
                      ? styles.navSectionTitleActive
                      : ""
                  }`}
                >
                  Part {taskIndex + 1}
                </div>

                {currentTask === taskIndex && (
                  <div className={styles.navQuestions}>
                    {Array.from({ length: count }, (_, qIndex) => {
                      const qNum = qIndex + 1;
                      const answered = isAnswered(task.readingId, qNum);

                      return (
                        <button
                          type="button"
                          key={`${taskIndex}-${qIndex}`}
                          className={[
                            styles.navButton,
                            answered
                              ? styles.completedNavButton
                              : styles.unansweredNavButton,
                            qIndex === 0 ? styles.activeNavButton : "",
                          ].join(" ")}
                          title={answered ? "Answered" : "Unanswered"}
                          onClick={(e) => {
                            e.stopPropagation();
                            setCurrentTask(taskIndex);
                            document
                              .querySelector(`.${styles.examWrapper}`)
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
          className={`${styles.highlightButton} ${
            highlightMode ? styles.highlightButtonActive : ""
          }`}
          onClick={toggleHighlightMode}
        >
          <Highlighter size={18} style={{ marginRight: "6px" }} />
          {highlightMode ? "Highlighting" : "Highlight"}
        </button>

        <button
          className={styles.completeButton}
          onClick={handleSubmit}
          disabled={isSubmitting}
        >
          {isSubmitting ? "Submitting..." : "Complete"}
        </button>
      </div>
    </div>
  );
}
