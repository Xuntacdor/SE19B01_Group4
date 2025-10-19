import React, { useEffect, useState, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { submitReadingAttempt } from "../../Services/ReadingApi";
import styles from "./ReadingExamPage.module.css";

export default function ReadingExamPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const { exam, tasks, duration } = state || {};

  const [page, setPage] = useState(0);
  const [submitted, setSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [timeLeft, setTimeLeft] = useState(duration ? duration * 60 : 0);

  const formRefs = useRef([]);

  // 🕒 Timer logic
  useEffect(() => {
    if (!timeLeft || submitted) return;
    const timer = setInterval(() => setTimeLeft((t) => Math.max(0, t - 1)), 1000);
    return () => clearInterval(timer);
  }, [timeLeft, submitted]);

  const formatTime = (sec) => {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${m}:${s < 10 ? "0" + s : s}`;
  };

  const handleNext = () => page < tasks.length - 1 && setPage((p) => p + 1);
  const handlePrev = () => page > 0 && setPage((p) => p - 1);

  // 📝 Submission logic
  const handleSubmit = (e) => {
    e?.preventDefault();
    if (isSubmitting) return;
    setIsSubmitting(true);

    const structuredAnswers = tasks.map((task, index) => {
      const form = formRefs.current[index];
      if (!form) return { skillId: task.readingId, answers: [] };

      const formData = new FormData(form);
      const values = [];
      for (let [, value] of formData.entries()) {
        if (value && value.trim() !== "") values.push(value.trim());
      }
      return { skillId: task.readingId, answers: values };
    });

    const anyEmpty = structuredAnswers.some((x) => x.answers.length === 0);
    if (anyEmpty) {
      alert("Please complete all questions before submitting.");
      setIsSubmitting(false);
      return;
    }

    const answerText = JSON.stringify(structuredAnswers);
    const attempt = {
      examId: exam.examId,
      answerText,
      startedAt: new Date().toISOString(),
    };

    submitReadingAttempt(attempt)
      .then((res) => {
        console.log(`✅ Reading submitted:`, res.data);
        setSubmitted(true);
      })
      .catch((err) => {
        console.error("❌ Submit failed:", err);
        alert(`Failed to submit your reading attempt.`);
      })
      .finally(() => setIsSubmitting(false));
  };

  // 🧭 Render
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

  return (
    <div className={styles.examWrapper}>
      <div className={styles.topBar}>
        <button className={styles.backBtn} onClick={() => navigate("/reading")}>
          ← Back
        </button>
        <h2 className={styles.examTitle}>{exam.examName}</h2>
        <div className={styles.timer}>⏱️ {formatTime(timeLeft)}</div>
      </div>

      {tasks.map((task, idx) => (
        <form
          key={idx}
          ref={(el) => (formRefs.current[idx] = el)}
          className={`${styles.examForm} ${
            idx === page ? styles.activeForm : styles.hiddenForm
          }`}
        >
          <div className={styles.questionPage}>
            <h3 className={styles.questionTitle}>
              Reading #{task.displayOrder || idx + 1}
            </h3>

            <div
              className={styles.readingContent}
              dangerouslySetInnerHTML={{ __html: task.readingContent || "" }}
            />

            <div
              className={styles.question}
              dangerouslySetInnerHTML={{
                __html: task.questionHtml,
              }}
            />
          </div>
        </form>
      ))}

      <div className={styles.navigation}>
        {page > 0 && (
          <button type="button" className={styles.navBtn} onClick={handlePrev}>
            ← Previous
          </button>
        )}
        {page < tasks.length - 1 ? (
          <button type="button" className={styles.navBtn} onClick={handleNext}>
            Next →
          </button>
        ) : (
          <button
            type="button"
            className={styles.submitBtn}
            onClick={handleSubmit}
            disabled={isSubmitting}
          >
            {isSubmitting ? "Submitting..." : "Submit All"}
          </button>
        )}
      </div>
    </div>
  );
}
