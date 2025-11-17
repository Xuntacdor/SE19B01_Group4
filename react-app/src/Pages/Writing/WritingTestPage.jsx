import React, { useState, useEffect, useCallback } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as WritingApi from "../../Services/WritingApi";
import LoadingComponent from "../../Components/Exam/LoadingComponent";
import styles from "./WritingTestPage.module.css";
import FloatDictionrary from "../../Components/Dictionary/FloatingDictionaryChat";
import ConfirmationPopup from "../../Components/Common/ConfirmationPopup";
export default function WritingTest() {
  const { state } = useLocation();
  const navigate = useNavigate();

  const [timeLeft, setTimeLeft] = useState(0);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [answers, setAnswers] = useState({});
  const [submitting, setSubmitting] = useState(false);
  const [started, setStarted] = useState(false);
  const [errorPopupOpen, setErrorPopupOpen] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  // state có thể undefined, nên destructure an toàn
  const { exam, tasks = [], task, mode } = state || {};
  const STORAGE_KEY = exam
    ? `writing_answers_exam_${exam.examId}`
    : "writing_answers_temp";

  // ===========================================
  // 1. RESTORE ANSWERS WHEN PAGE LOADS
  // ===========================================
  useEffect(() => {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      try {
        setAnswers(JSON.parse(saved));
      } catch {
        // ignore
      }
    }
  }, [STORAGE_KEY]);

  // ===========================================
  // 1.1 ENSURE ALL TASKS HAVE A KEY TRONG answers
  // (để Task 2 không bị mất)
  // ===========================================
  useEffect(() => {
    if (!tasks || tasks.length === 0) return;

    setAnswers((prev) => {
      const updated = { ...prev };
      tasks.forEach((t) => {
        if (updated[t.writingId] === undefined) {
          updated[t.writingId] = "";
        }
      });
      localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
      return updated;
    });
  }, [tasks, STORAGE_KEY]);

  // ===========================================
  // 2. SET TIMER
  // ===========================================
  useEffect(() => {
    if (!mode) return;

    if (mode === "full") {
      setTimeLeft(60 * 60);
      setStarted(true);
    } else if (task?.displayOrder === 1) {
      setTimeLeft(20 * 60);
      setStarted(true);
    } else if (task?.displayOrder === 2) {
      setTimeLeft(40 * 60);
      setStarted(true);
    }
  }, [mode, task]);

  // Countdown
  useEffect(() => {
    if (timeLeft <= 0) return;
    const timer = setInterval(() => setTimeLeft((t) => t - 1), 1000);
    return () => clearInterval(timer);
  }, [timeLeft]);

  const formatTime = (sec) => {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${m}:${s.toString().padStart(2, "0")}`;
  };

  // ===========================================
  // 3. CURRENT TASK
  // ===========================================
  const currentTask =
    mode === "full" ? tasks?.[currentIndex] : task || tasks?.[0];

  const currentId =
    mode === "full"
      ? tasks?.[currentIndex]?.writingId
      : task?.writingId || tasks?.[0]?.writingId;

  const currentAnswer = currentId ? answers[currentId] || "" : "";

  // ===========================================
  // 4. WORD COUNT LOGIC
  // ===========================================
  const getWordCount = (text) =>
    text.trim().length === 0
      ? 0
      : text
          .trim()
          .split(/\s+/)
          .filter((w) => w.length > 0).length;

  const wordCount = getWordCount(currentAnswer);
  const wordLimit = currentTask?.displayOrder === 1 ? 150 : 250;
  const isEnough = wordCount >= wordLimit;

  // ===========================================
  // 5. AUTO-SAVE ANSWERS
  // ===========================================
  const handleChange = (e) => {
    const text = e.target.value;
    if (!currentId) return;

    const updated = {
      ...answers,
      [currentId]: text,
    };

    setAnswers(updated);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
  };

  // Next / Prev task (full mode)
  const handleNext = () => {
    if (currentIndex < tasks.length - 1) setCurrentIndex((i) => i + 1);
  };

  const handlePrev = () => {
    if (currentIndex > 0) setCurrentIndex((i) => i - 1);
  };

  // ===========================================
  // 6. SUBMIT HANDLER (FIXED WITH useCallback)
  // ===========================================
  const handleSubmit = useCallback(async () => {
    if (submitting) return; // prevent double submit

    try {
      setSubmitting(true);

      const user = JSON.parse(localStorage.getItem("user"));
      if (!user) {
        alert("Please log in to submit your test.");
        return;
      }

      const usedTasks = mode === "full" ? tasks : [task];


      const mergedAnswer =
        usedTasks
          .sort((a, b) => a.displayOrder - b.displayOrder)
          .map((t) => {
            const text = answers[t.writingId] || "";
            return `--- TASK ${t.displayOrder} ---\n${text.trim()}`;
          })
          .join("\n\n");

      const gradeData = {
        examId: exam.examId,
        mode,
        answers: usedTasks.map((t) => ({
          writingId: t.writingId,
          displayOrder: t.displayOrder,
          answerText: answers[t.writingId] || "",
          imageUrl: t.displayOrder === 1 ? t.imageUrl : null
        })),
        answerText: mergedAnswer
      };

      await WritingApi.gradeWriting(gradeData);

      // clear autosave cho exam này
      localStorage.removeItem(STORAGE_KEY);

      navigate("/writing/result", {
        state: {
          examId: exam.examId,
          userId: user.userId,
          exam,
          mode,
          originalAnswers: answers,
          isWaiting: true,
        },
      });
    } catch (err) {
      console.error("Submit failed:", err);

      setErrorMessage(
        err?.response?.data?.error || "Error while submitting the essay."
      );
      setErrorPopupOpen(true);
    } finally {
      setSubmitting(false);
    }
  }, [submitting, exam, mode, tasks, task, answers, navigate, STORAGE_KEY]);

  // ===========================================
  // 7. AUTO-SUBMIT WHEN TIME EXPIRES
  // ===========================================
  useEffect(() => {
    if (!started) return; // prevent instant submit
    if (timeLeft === 0 && !submitting) {
      handleSubmit();
    }
  }, [timeLeft, submitting, started, handleSubmit]);


  if (!state) {
    return (
      <AppLayout title="Writing Test" sidebar={<GeneralSidebar />}>
        <div className={styles.center}>
          <h2>No exam selected</h2>
          <button onClick={() => navigate(-1)} className={styles.backBtn}>
            ← Back
          </button>
        </div>
      </AppLayout>
    );
  }

  // ===========================================
  // 9. RENDER
  // ===========================================
  return (
    <AppLayout title="Writing Test" sidebar={<GeneralSidebar />}>
      <div className={styles.container}>
        <div className={styles.header}>
          <h2>
            {mode === "full"
              ? `Full Writing Test — ${exam.examName}`
              : `Task ${currentTask?.displayOrder} — ${exam.examName}`}
          </h2>
          <div className={styles.timer}>⏰ {formatTime(timeLeft)}</div>
        </div>

        <div className={styles.splitLayout}>
          {/* LEFT SIDE */}
          <div className={styles.leftPane}>
            <div className={styles.answerHeader}>
              <h4>Your Answer:</h4>
              <div
                className={`${styles.wordCount} ${isEnough ? styles.wordOK : styles.wordLow
                  }`}
              >
                Words: {wordCount} / {wordLimit}
              </div>
            </div>

            <textarea
              placeholder="Start writing your essay here..."
              className={styles.textarea}
              value={currentAnswer}
              onChange={handleChange}
            />

            <div className={styles.actions}>
              <button
                className={styles.submitBtn}
                onClick={handleSubmit}
                disabled={submitting}
              >
                Submit
              </button>

              <button className={styles.backBtn} onClick={() => navigate(-1)}>
                ← Back
              </button>
            </div>
          </div>

          {/* RIGHT SIDE */}
          <div className={styles.rightPane}>
            <div className={styles.taskBlock}>
              <h3>Task {currentTask?.displayOrder}</h3>
              <p>{currentTask?.writingQuestion}</p>

              {currentTask?.imageUrl && (
                <img
                  src={currentTask.imageUrl}
                  alt="Task"
                  className={styles.taskImage}
                />
              )}
            </div>

            {mode === "full" && (
              <div className={styles.switchButtons}>
                {currentIndex > 0 && (
                  <button onClick={handlePrev} className={styles.switchBtn}>
                    ← Task {currentIndex}
                  </button>
                )}
                {currentIndex < tasks.length - 1 && (
                  <button onClick={handleNext} className={styles.switchBtn}>
                    Task {currentIndex + 2} →
                  </button>
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      <FloatDictionrary />
      {submitting && <LoadingComponent text="Submitting your essay..." />}
      <ConfirmationPopup
        isOpen={errorPopupOpen}
        onClose={() => setErrorPopupOpen(false)}
        onConfirm={() => setErrorPopupOpen(false)}
        title="Submission Failed"
        message={errorMessage}
        confirmText="OK"
        cancelText="Cancel"
        type="danger"
      />
    </AppLayout>
  );
}
