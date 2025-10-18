import React, { useEffect, useState, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { submitAttempt } from "../../Services/ExamApi";
import styles from "./ListeningExamPage.module.css";

export default function ListeningExamPage() {
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
      if (!form) return { skillId: task.listeningId, answers: [] };

      const formData = new FormData(form);
      const values = [];
      for (let [, value] of formData.entries()) {
        if (value && value.trim() !== "") values.push(value.trim());
      }
      return { skillId: task.listeningId, answers: values };
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

    submitAttempt(attempt)
      .then((res) => {
        console.log(`✅ Listening submitted:`, res.data);
        setSubmitted(true);
      })
      .catch((err) => {
        console.error("❌ Submit failed:", err);
        alert(`Failed to submit your listening attempt.`);
      })
      .finally(() => setIsSubmitting(false));
  };

  // 🧭 Render
  if (!exam)
    return (
      <div className={styles.fullscreenCenter}>
        <h2>No exam selected</h2>
        <button className={styles.backBtn} onClick={() => navigate("/listening")}>
          ← Back
        </button>
      </div>
    );

  if (submitted)
    return (
      <div className={styles.fullscreenCenter}>
        <h3>✅ Listening Test Submitted!</h3>
        <p>Your answers have been recorded successfully.</p>
        <button className={styles.backBtn} onClick={() => navigate("/listening")}>
          ← Back to Listening List
        </button>
      </div>
    );

  return (
    <div className={styles.examWrapper}>
      <div className={styles.topBar}>
        <button className={styles.backBtn} onClick={() => navigate("/listening")}>
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
              Listening #{task.displayOrder || idx + 1}
            </h3>

            <div className={styles.audioSection}>
              {task.audioUrl ? (
                <audio controls className={styles.audioPlayer}>
                  <source src={task.audioUrl} type="audio/mpeg" />
                  Your browser does not support the audio element.
                </audio>
              ) : (
                <p>No audio available.</p>
              )}
            </div>

            <div
              className={styles.question}
              dangerouslySetInnerHTML={{
                __html: task.questionHtml || task.listeningQuestion,
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
