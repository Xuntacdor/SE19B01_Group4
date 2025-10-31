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
        alert(`You can only select ${limit} option${limit > 1 ? "s" : ""}.`);
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

  // ✅ Helper to count questions
  const getQuestionCount = (questionText) => {
    if (!questionText) return 0;
    const matches = questionText.match(/\[!num\]/g);
    return matches ? matches.length : 0;
  };

  // ✅ Submit logic (force-complete removed)
  const handleSubmit = (e) => {
    e?.preventDefault();
    if (isSubmitting) return;

    const structuredAnswers = tasks.map((task) => {
      const prefix = `${task.listeningId}_q`;
      const questionKeys = Object.keys(answers)
        .filter((k) => k.startsWith(prefix))
        .sort((a, b) => {
          const na = parseInt(a.split("_q")[1]) || 0;
          const nb = parseInt(b.split("_q")[1]) || 0;
          return na - nb;
        });

      const taskAnswers = questionKeys.map((key) => {
        const val = answers[key];
        if (Array.isArray(val)) return val.join(", ");
        return val?.trim() || "_";
      });

      return { SkillId: task.listeningId, Answers: taskAnswers };
    });

    setIsSubmitting(true);
    const jsonString = JSON.stringify(structuredAnswers);
    const attempt = {
      examId: exam.examId,
      startedAt: new Date().toISOString(),
      answers: jsonString,
    };

    submitListeningAttempt(attempt)
      .then((res) => {
        const attempt = res.data;
        navigate("/listening/result", {
          state: {
            attemptId: attempt.attemptId,
            examName: attempt.examName,
            isWaiting: true,
          },
        });
        setSubmitted(true);
      })
      .catch((err) => {
        console.error("❌ Submit failed:", err);
        alert(`Failed to submit your listening attempt.\n\n${jsonString}`);
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

  const currentTaskData = tasks[currentTask];
  const questionCount = getQuestionCount(currentTaskData?.listeningQuestion);

  return (
    <div className={styles.examWrapper}>
      {/* ===== Header ===== */}
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

      {/* ===== Main Content ===== */}
      <div className={styles.mainContent}>
        <div className={styles.leftPanel}>
          {currentTaskData?.listeningContent ? (
            <div className={styles.audioContainer}>
              <h3 className={styles.audioTitle}>Audio Track</h3>
              <audio controls className={styles.audioPlayer}>
                <source src={currentTaskData.listeningContent} type="audio/mpeg" />
                Your browser does not support the audio element.
              </audio>
            </div>
          ) : (
            <div className={styles.noAudio}>No audio available for this task.</div>
          )}
        </div>

        <div className={styles.rightPanel}>
          <form ref={formRef} onChange={handleChange} onInput={handleChange}>
            {currentTaskData?.listeningQuestion ? (
              <ExamMarkdownRenderer
                markdown={currentTaskData.listeningQuestion}
                showAnswers={false}
                readingId={currentTaskData.listeningId}
              />
            ) : (
              <div className={styles.noQuestionBox}>
                <h3>No Questions Found</h3>
                <p>This listening section has no questions configured.</p>
              </div>
            )}
          </form>
        </div>
      </div>

      {/* ===== Footer Navigation ===== */}
      <div className={styles.bottomNavigation}>
        <div className={styles.navScrollContainer}>
          {tasks.map((task, taskIndex) => {
            const count = getQuestionCount(task.listeningQuestion);
            return (
              <div key={task.listeningId} className={styles.navSection}>
                <div className={styles.navSectionTitle}>Part {taskIndex + 1}</div>
                <div className={styles.navQuestions}>
                  {Array.from({ length: count }, (_, qIndex) => (
                    <button
                      key={`${taskIndex}-${qIndex}`}
                      className={`${styles.navButton} ${
                        currentTask === taskIndex ? styles.activeNavButton : ""
                      }`}
                      onClick={() => setCurrentTask(taskIndex)}
                    >
                      {qIndex + 1}
                    </button>
                  ))}
                </div>
              </div>
            );
          })}
        </div>

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
