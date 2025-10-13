import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import styles from "./WritingTestPage.module.css";
import * as WritingApi from "../../Services/WritingApi";

export default function WritingTest() {
  const { state } = useLocation();
  const navigate = useNavigate();

  const [timeLeft, setTimeLeft] = useState(0);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [answers, setAnswers] = useState({});
  const [feedbacks, setFeedbacks] = useState([]);
  const [averageBand, setAverageBand] = useState(null);
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

  // ========== TIMER ==========
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

  // ========== TASK ==========
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

  // ========== SUBMIT ==========
  const handleSubmit = async () => {
    try {
      setSubmitting(true);
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

      await WritingApi.gradeWriting(gradeData);

      const user = JSON.parse(localStorage.getItem("user"));
      if (!user) {
        alert("Please log in to view feedback.");
        return;
      }

      const result = await WritingApi.getFeedback(exam.examId, user.userId);
      if (!result?.feedbacks) return;

      const latest = Object.values(
        result.feedbacks.reduce((acc, f) => {
          acc[f.writingId] = acc[f.writingId] || [];
          acc[f.writingId].push(f);
          return acc;
        }, {})
      ).map((arr) =>
        arr.sort(
          (a, b) =>
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        )[0]
      );

      setFeedbacks(latest.slice(0, mode === "full" ? 2 : 1));
      setAverageBand(result.averageOverall);
    } catch (err) {
      console.error("Grading failed:", err);
      alert("Error while grading the essay!");
    } finally {
      setSubmitting(false);
    }
  };

  // ========== RENDER ==========
  return (
    <AppLayout title="Writing Test">
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
          {/* ===== LEFT SIDE ===== */}
          <div className={styles.leftPane}>
            <div className={styles.answerHeader}>
              <h4>Your Answer:</h4>
              <div
                className={`${styles.wordCount} ${isEnough ? styles.wordOK : styles.wordLow}`}
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
                {submitting ? "Submitting..." : "Submit"}
              </button>
              <button className={styles.backBtn} onClick={() => navigate(-1)}>
                ← Back
              </button>
            </div>

            {/* ===== FEEDBACK ===== */}
            {feedbacks.length > 0 && (
              <div className={styles.resultBox}>
                <h3>💯 Latest Feedback</h3>
                <p>
                  <b>Average Band:</b> {averageBand}
                </p>

                {feedbacks.map((f, i) => {
                  const feedbackParsed = JSON.parse(f.feedbackSections || "{}");
                  const grammarParsed = JSON.parse(f.grammarVocabJson || "{}");

                  return (
                    <div key={i} className={styles.feedbackBlock}>
                      <h4>Task {i + 1}</h4>
                      <ul>
                        <li>Task Achievement: {f.taskAchievement}</li>
                        <li>Coherence & Cohesion: {f.coherenceCohesion}</li>
                        <li>Lexical Resource: {f.lexicalResource}</li>
                        <li>Grammar Accuracy: {f.grammarAccuracy}</li>
                        <li>
                          <b>Overall:</b> {f.overall}
                        </li>
                      </ul>

                      {/* Grammar & Vocab Accordion */}
                      <details className={styles.detailsBlock}>
                        <summary>📘 Grammar & Vocabulary</summary>
                        <p><b>Overview:</b> {grammarParsed.overview}</p>
                        {Array.isArray(grammarParsed.errors) && (
                          <ul className={styles.errorList}>
                            {grammarParsed.errors.map((err, idx) => (
                              <li key={idx} className={styles.errorItem}>
                                <details>
                                  <summary>
                                    <b>{err.category}</b>: "{err.incorrect}" → <i>{err.suggestion}</i>
                                  </summary>
                                  <small>{err.explanation}</small>
                                </details>
                              </li>
                            ))}
                          </ul>
                        )}
                      </details>

                      {/* Task Feedback Accordion */}
                      <details className={styles.detailsBlock}>
                        <summary>🗒 Task Feedback Details</summary>
                        <p><b>Overview:</b> {feedbackParsed.overview}</p>
                        {Array.isArray(feedbackParsed.paragraph_feedback) && (
                          <ul>
                            {feedbackParsed.paragraph_feedback.map((p, idx) => (
                              <li key={idx}>
                                <b>{p.section}</b>
                                <ul>
                                  <li>✅ Strengths: {p.strengths.join(", ")}</li>
                                  <li>⚠️ Weaknesses: {p.weaknesses.join(", ")}</li>
                                  <li>💡 Advice: {p.advice}</li>
                                </ul>
                              </li>
                            ))}
                          </ul>
                        )}
                      </details>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* ===== RIGHT SIDE ===== */}
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
    </AppLayout>
  );
}
