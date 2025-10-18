import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as WritingApi from "../../Services/WritingApi";
import LoadingComponent from "../../Components/Exam/LoadingComponent"; // ✅ import here
import styles from "./WritingTestPage.module.css";

export default function WritingTest() {
  const { state } = useLocation();
  const navigate = useNavigate();

  const [timeLeft, setTimeLeft] = useState(0);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [answers, setAnswers] = useState({});
  const [submitting, setSubmitting] = useState(false);

  if (!state)
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

  const { exam, tasks, task, mode } = state;

  // Timer logic
  useEffect(() => {
    if (mode === "full") setTimeLeft(60 * 60);
    else if (task?.displayOrder === 1) setTimeLeft(20 * 60);
    else if (task?.displayOrder === 2) setTimeLeft(40 * 60);
  }, [mode, task]);

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

  const currentTask =
    mode === "full" && Array.isArray(tasks) ? tasks[currentIndex] : task;

  const currentId = mode === "full" ? currentTask?.writingId : task?.writingId;
  const currentAnswer = answers[currentId] || "";

  const getWordCount = (text) =>
    text.trim().length === 0
      ? 0
      : text.trim().split(/\s+/).filter((w) => w.length > 0).length;

  const wordCount = getWordCount(currentAnswer);
  const wordLimit = currentTask?.displayOrder === 1 ? 150 : 250;
  const isEnough = wordCount >= wordLimit;

  const handleChange = (e) => {
    const text = e.target.value;
    setAnswers((prev) => ({
      ...prev,
      [currentId]: text,
    }));
  };

  const handleNext = () => {
    if (currentIndex < tasks.length - 1) setCurrentIndex((i) => i + 1);
  };
  const handlePrev = () => {
    if (currentIndex > 0) setCurrentIndex((i) => i - 1);
  };

  // ✅ Modified submit handler
  const handleSubmit = async () => {
    try {
      setSubmitting(true); // show loading
      const user = JSON.parse(localStorage.getItem("user"));
      if (!user) {
        alert("Please log in to submit your test.");
        return;
      }

      const gradeData = {
        examId: exam.examId,
        mode,
        answers: (mode === "full" ? tasks : [task]).map((t) => ({
          writingId: t.writingId,
          displayOrder: t.displayOrder,
          answerText: answers[t.writingId] || "",
          imageUrl: t.displayOrder === 1 ? t.imageUrl || null : null,
        })),
      };

      // Send essay for grading
      await WritingApi.gradeWriting(gradeData);

      // Navigate to result page
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
      alert("Error while submitting the essay.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <AppLayout title="Writing Test" sidebar={<GeneralSidebar />}>
      <div className={styles.container}>
        <div className={styles.header}>
          <h2>
            {mode === "full"
              ? `Full Writing Test — ${exam.examName}`
              : `Task ${task.displayOrder} — ${exam.examName}`}
          </h2>
          <div className={styles.timer}>⏰ {formatTime(timeLeft)}</div>
        </div>

        <div className={styles.splitLayout}>
          <div className={styles.leftPane}>
            <div className={styles.answerHeader}>
              <h4>Your Answer:</h4>
              <div
                className={`${styles.wordCount} ${
                  isEnough ? styles.wordOK : styles.wordLow
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

      {/* ✅ Overlay spinner shown while submitting */}
      {submitting && <LoadingComponent text="Submitting your essay..." />}
    </AppLayout>
  );
}
