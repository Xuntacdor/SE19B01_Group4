import React, { useEffect, useState, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { submitListeningAttempt } from "../../Services/ListeningApi";
import ExamMarkdownRenderer from "../../Components/Exam/ExamMarkdownRenderer";
import { Clock, Headphones } from "lucide-react";
import styles from "./ListeningExamPage.module.css";

export default function ListeningExamPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const { exam, tasks, duration } = state || {};

  const [submitted, setSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [timeLeft, setTimeLeft] = useState(duration ? duration * 60 : 0);
  const [answers, setAnswers] = useState({});
  const [currentTask, setCurrentTask] = useState(0);

  const formRef = useRef(null);

  // Timer
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

  // Submit
  const handleSubmit = (e) => {
    e?.preventDefault();
    if (isSubmitting) return;
    setIsSubmitting(true);

    const structuredAnswers = tasks.map((task) => {
      const taskAnswers = [];
      Object.keys(answers).forEach((key) => {
        if (key.startsWith(`${task.listeningId}_`)) {
          taskAnswers.push(answers[key]);
        }
      });
      return { skillId: task.listeningId, answers: taskAnswers };
    });

    const attempt = {
      examId: exam.examId,
      startedAt: new Date().toISOString(),
      answers: structuredAnswers,
    };

    submitListeningAttempt(attempt)
      .then((res) => {
        console.log("✅ Listening submitted:", res.data);
        setSubmitted(true);
      })
      .catch((err) => {
        console.error("❌ Submit failed:", err);
        alert("Failed to submit your listening attempt.");
      })
      .finally(() => setIsSubmitting(false));
  };

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

  const task = tasks[currentTask];
  const getQuestionCount = (q) => {
    if (!q) return 0;
    const matches = q.match(/\b\d+\b/g);
    return matches ? Math.min(Math.max(...matches), 40) : 0;
  };
  const questionCount = getQuestionCount(task?.listeningQuestion);

  return (
    <div className={styles.examWrapper}>
      {/* === Header === */}
      <div className={styles.topHeader}>
        <button className={styles.backBtn} onClick={() => navigate("/listening")}>
          ← Back
        </button>
        <h2 className={styles.examTitle}>
          <Headphones size={22} style={{ marginRight: 6 }} /> {exam.examName}
        </h2>
        <div className={styles.timer}>
          <Clock size={20} /> {formatTime(timeLeft)}
        </div>
      </div>

      {/* === Main === */}
      <div className={styles.mainContent}>
        {/* Left panel: only audio */}
        <div className={styles.leftPanel}>
          {task?.audioUrl ? (
            <div className={styles.audioContainer}>
              <h3 className={styles.audioTitle}>Audio Track</h3>
              <audio controls className={styles.audioPlayer}>
                <source src={task.audioUrl} type="audio/mpeg" />
                Your browser does not support the audio element.
              </audio>
            </div>
          ) : (
            <div className={styles.noAudio}>No audio available for this task.</div>
          )}
        </div>

        {/* Right panel: Questions */}
        <div className={styles.rightPanel}>
          <form ref={formRef}>
            {task?.listeningQuestion ? (
              <ExamMarkdownRenderer markdown={task.listeningQuestion} showAnswers={false} />
            ) : (
              <div className={styles.noQuestionBox}>
                <h3>No Questions Found</h3>
                <p>This listening section has no questions configured.</p>
              </div>
            )}
          </form>
        </div>
      </div>

      {/* === Submit Section === */}
      <div className={styles.submitSection}>
        <button
          className={styles.submitButton}
          onClick={handleSubmit}
          disabled={isSubmitting}
        >
          {isSubmitting ? "Submitting..." : "Complete"}
        </button>
      </div>
    </div>
  );
}
