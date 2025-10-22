import React, { useEffect, useState, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { submitReadingAttempt } from "../../Services/ReadingApi";
import ExamMarkdownRenderer from "../../Components/Exam/ExamMarkdownRenderer";
import { Clock } from "lucide-react";
import styles from "./ReadingExamPage.module.css";

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

  // 🕒 Timer
  useEffect(() => {
    if (!timeLeft || submitted) return;
    const timer = setInterval(
      () => setTimeLeft((t) => Math.max(0, t - 1)),
      1000
    );
    return () => clearInterval(timer);
  }, [timeLeft, submitted]);

  // 🧩 Dynamic input capture (with checkbox limit enforcement)
  useEffect(() => {
    const form = formRef.current;
    if (!form) return;

    const handleInput = (e) => {
      const { name, value, type, checked, dataset } = e.target;
      if (!name) return;

      // Handle multi-choice checkboxes with proper limit enforcement
      if (type === "checkbox") {
        const limit = parseInt(dataset.limit || "0", 10);
        const allInGroup = form.querySelectorAll(`input[name="${name}"][type="checkbox"]`);
        const checkedInGroup = Array.from(allInGroup).filter((el) => el.checked);

        if (checked && limit && checkedInGroup.length > limit) {
          e.preventDefault();
          e.stopImmediatePropagation();
          e.target.checked = false;
          alert(`You can only select ${limit} option${limit > 1 ? "s" : ""} for this question.`);
          return;
        }

        const selected = Array.from(allInGroup)
          .filter((el) => el.checked)
          .map((el) => el.value);

        setAnswers((prev) => ({ ...prev, [name]: selected }));
        return;
      }

      // Handle radio
      if (type === "radio") {
        if (checked) setAnswers((prev) => ({ ...prev, [name]: [value] }));
        return;
      }

      // Handle text & dropdown
      setAnswers((prev) => ({ ...prev, [name]: [value] }));
    };

    form.addEventListener("input", handleInput);
    form.addEventListener("change", handleInput);
    return () => {
      form.removeEventListener("input", handleInput);
      form.removeEventListener("change", handleInput);
    };
  }, [currentTask]);

  const formatTime = (sec) => {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${m}:${s < 10 ? "0" + s : s}`;
  };

  const handleQuestionNavigation = (questionNumber) => {
    setCurrentTask(questionNumber - 1);
  };

  // 📝 Submit logic
  const handleSubmit = (e) => {
    e?.preventDefault();
    if (isSubmitting) return;
    setIsSubmitting(true);

    const structuredAnswers = tasks.map((task) => {
      const taskAnswers = [];
      Object.keys(answers).forEach((key) => {
        if (key.startsWith(`${task.readingId}_`)) {
          const val = answers[key];
          if (Array.isArray(val)) taskAnswers.push(...val);
          else taskAnswers.push(val);
        }
      });

      return {
        SkillId: task.readingId,
        Answers: taskAnswers.length > 0 ? taskAnswers : ["_"],
      };
    });

    // ✅ Convert to exact JSON string format expected by backend
    const jsonString = JSON.stringify(structuredAnswers);

    const attempt = {
      examId: exam.examId,
      startedAt: new Date().toISOString(),
      answers: jsonString, // send as string
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
      .catch((err) => {
        console.error("❌ Submit failed:", err);
        alert(
          `Failed to submit your reading attempt.\n\n${jsonString}`
        );
      })
      .finally(() => setIsSubmitting(false));
  };

  const getQuestionCount = (readingQuestion) => {
    if (!readingQuestion) return 0;
    const end = readingQuestion.match(/(\d+)\s*$/gm);
    const numMarkers = readingQuestion.match(/\[!num\]\s*(\d+)/g);
    const ranges = readingQuestion.match(/Questions?\s+(\d+)\s*-\s*(\d+)/gi);
    let nums = [];
    if (end) nums.push(...end.map(Number));
    if (numMarkers)
      nums.push(
        ...numMarkers.map((m) => {
          const n = m.match(/(\d+)/);
          return n ? Number(n[1]) : 0;
        })
      );
    if (ranges)
      ranges.forEach((r) => {
        const m = r.match(/(\d+)\s*-\s*(\d+)/i);
        if (m) for (let i = Number(m[1]); i <= Number(m[2]); i++) nums.push(i);
      });
    const valid = nums.filter((n) => n >= 1 && n <= 50);
    return valid.length > 0 ? Math.max(...valid) : 0;
  };

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

  const currentTaskData = tasks[currentTask];
  const questionCount = getQuestionCount(currentTaskData?.readingQuestion);

  return (
    <div className={styles.examWrapper}>
      <div className={styles.topHeader}>
        <button className={styles.backBtn} onClick={() => navigate("/reading")}>
          ← Back
        </button>
        <h2 className={styles.examTitle}>{exam.examName}</h2>
        <div className={styles.timer}>
          <Clock size={20} />
          {formatTime(timeLeft)}
        </div>
      </div>

      <div className={styles.mainContent}>
        <div className={styles.leftPanel}>
          {currentTaskData?.passageTitle && (
            <div className={styles.passageHeader}>
              <h3 className={styles.passageTitle}>
                {currentTaskData.passageTitle}
              </h3>
            </div>
          )}
          <p className={styles.passageContent}> 
            {currentTaskData?.readingContent || ""}
          </p>
        </div>
        <div className={styles.rightPanel}>
          <form ref={formRef}>
            {currentTaskData?.readingQuestion ? (
              <ExamMarkdownRenderer
                markdown={currentTaskData.readingQuestion}
                showAnswers={false}
                readingId={currentTaskData.readingId}
              />
            ) : (
              <div className={styles.questionSection}>
                <div style={{ padding: "40px", textAlign: "center", color: "#666" }}>
                  <h3>No Questions Found</h3>
                  <p>This reading test doesn't have any questions configured yet.</p>
                </div>
              </div>
            )}
          </form>
        </div>
      </div>

      <div className={styles.bottomNavigation}>
        {Array.from({ length: questionCount }, (_, i) => i + 1).map((num) => (
          <button
            key={num}
            className={`${styles.navButton} ${
              currentTask === num - 1 ? styles.activeNavButton : ""
            }`}
            onClick={() => handleQuestionNavigation(num)}
          >
            {num}
          </button>
        ))}
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
