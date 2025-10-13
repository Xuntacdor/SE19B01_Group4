import React from "react";
import { useLocation, useNavigate } from "react-router-dom";
import ExamSkillPage from "../../Components/Exam/ExamSkillPage";
import styles from "./ReadingExamPage.module.css";

export default function ReadingExamPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const { exam, tasks, duration } = state || {};

  const { exam, tasks = [], duration } = state || {};

  const [page, setPage] = useState(0);
  const [submitted, setSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [timeLeft, setTimeLeft] = useState(duration ? duration * 60 : 0);

  const formRefs = useRef([]);

  // ========== TIMER ==========
  useEffect(() => {
    if (!timeLeft || submitted) return;
    const timer = setInterval(
      () => setTimeLeft((t) => Math.max(0, t - 1)),
      1000
    );
    return () => clearInterval(timer);
  }, [timeLeft, submitted]);

  const formatTime = (sec) => {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${m}:${s < 10 ? "0" + s : s}`;
  };

  const handleNext = () => {
    if (page < tasks.length - 1) setPage((p) => p + 1);
  };
  const handlePrev = () => {
    if (page > 0) setPage((p) => p - 1);
  };

  const handleSubmit = (e) => {
    e?.preventDefault();
    if (isSubmitting) return;
    setIsSubmitting(true);

    // ✅ Build structured answers by task/skill
    const structuredAnswers = tasks.map((task, index) => {
      const form = formRefs.current[index];
      if (!form) return { skillId: task.readingId, answers: [] };

      const formData = new FormData(form);
      const values = [];
      for (let [, value] of formData.entries()) {
        if (value && value.trim() !== "") values.push(value.trim());
      }

      return {
        skillId: task.readingId,
        answers: values,
      };
    });

    // ✅ Validation check
    const anyEmpty = structuredAnswers.some((x) => x.answers.length === 0);
    if (anyEmpty) {
      alert("Please complete all questions before submitting.");
      setIsSubmitting(false);
      return;
    }

    // ✅ Convert to JSON string (keeps it inside dto.AnswerText)
    const answerText = JSON.stringify(structuredAnswers);
    const attempt = {
      examId: exam.examId,
      answerText,
      startedAt: new Date().toISOString(),
    };

    // ✅ Non-async version (Promise chain)
    submitAttempt(attempt)
      .then((res) => {
        console.log("✅ Submitted structured attempt:", res.data);
        setSubmitted(true);
      })
      .catch((err) => {
        console.error("❌ Submit failed:", err);
        alert("Failed to submit your reading attempt. Please try again.");
      })
      .finally(() => {
        setIsSubmitting(false);
      });
  };

  // ========== RENDER ==========
  if (!exam) {
    return (
      <div className={styles.fullscreenCenter}>
        <h2>No exam selected</h2>
        <button className={styles.backBtn} onClick={() => navigate(-1)}>
          ← Back
        </button>
      </div>
    );
  }

  if (submitted) {
    return (
      <div className={styles.fullscreenCenter}>
        <h3>✅ Reading Test Submitted!</h3>
        <p>Your answers have been recorded successfully.</p>
        <button className={styles.backBtn} onClick={() => navigate("/reading")}>
          ← Back to Reading List
        </button>
      </div>
    );
  }

  return (
    <ExamSkillPage
      exam={exam}
      tasks={tasks}
      duration={duration}
      skillKey="readingId"
      skillType="Reading"
      renderContent={(task) => (
        <div
          className={styles.readingContent}
          dangerouslySetInnerHTML={{ __html: task.readingContent || "" }}
        />
      )}
      renderQuestion={(task) => (
        <div
          className={styles.question}
          dangerouslySetInnerHTML={{
            __html: task.questionHtml || task.readingQuestion,
          }}
        />
      )}
      onBack={() => navigate("/reading")}
    />
  );
}
