// ListeningExamPage.jsx
import React, { useEffect, useState, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { marked } from "marked";
import { submitListeningAttempt } from "../../Services/ListeningApi";
import ExamMarkdownRenderer from "../../Components/Exam/ExamMarkdownRenderer";
import { Clock } from "lucide-react";
import styles from "./ListeningExamPage.module.css";

// ---------- Markdown config ----------
marked.setOptions({
  gfm: true,
  breaks: true,
  mangle: false,
  headerIds: false,
});

// Strip [H*] wrappers (if any) and keep inner content
function passageMarkdownToHtml(raw) {
  if (!raw) return "";
  const cleaned = String(raw).replace(
    /\[H(?:\*([^\]]*))?\]([\s\S]*?)\[\/H\]/g,
    (_match, _id, inner) => inner
  );
  return marked.parse(cleaned);
}

export default function ListeningExamPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const { exam, tasks, duration } = state || {};

  const [currentTask, setCurrentTask] = useState(0);
  const [submitted, setSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [timeLeft, setTimeLeft] = useState(duration ? duration * 60 : 0);
  const [answers, setAnswers] = useState({});
  const formRef = useRef(null);

  // Countdown
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

  // Helper: is question answered?
  const isAnswered = (skillId, qNumber) => {
    const key = `${skillId}_q${qNumber}`;
    const val = answers[key];
    if (Array.isArray(val)) return val.length > 0;
    if (typeof val === "string") return val.trim() !== "";
    return false;
  };

  // Submit
  const handleSubmit = (e) => {
    e?.preventDefault();
    if (isSubmitting) return;

    const structuredAnswers = (tasks || []).map((task) => {
      const prefix = `${task.listeningId}_q`;
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
        else if (typeof val === "string" && val.trim() !== "") taskAnswers[key] = val;
        else taskAnswers[key] = "_";
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

  // Count [!num] markers
  const getQuestionCount = (listeningQuestion) => {
    if (!listeningQuestion) return 0;
    const numMarkers = listeningQuestion.match(/\[!num\]/g);
    return numMarkers ? numMarkers.length : 0;
  };

  // Scroll top when switching parts
  useEffect(() => {
    document.querySelector(`.${styles.examWrapper}`)?.scrollTo({ top: 0, behavior: "smooth" });
  }, [currentTask]);

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

  const currentTaskData = (tasks || [])[currentTask];
  const questionCount = getQuestionCount(currentTaskData?.listeningQuestion);

  return (
    <div className={styles.examWrapper}>
      {/* Header */}
      <div className={styles.topHeader}>
        <button className={styles.backBtn} onClick={() => navigate("/listening")}>
          ← Back
        </button>
        <h2 className={styles.examTitle}>{exam.examName}</h2>
        <div className={styles.timer}>
          <Clock size={20} />
          {formatTime(timeLeft)}
        </div>
      </div>

      {/* Middle rail (panel stays in the middle, content left-aligned) */}
      <div className={styles.middleGrid}>
        <div className={styles.middleRail}>
          <div className={styles.questionCard}>
            <form ref={formRef} onChange={handleChange} onInput={handleChange}>
              {currentTaskData?.passageTitle && (
                <h3 className={styles.passageTitle}>{currentTaskData.passageTitle}</h3>
              )}

              {currentTaskData?.listeningQuestion ? (
                <ExamMarkdownRenderer
                  markdown={currentTaskData.listeningQuestion}
                  showAnswers={false}
                  skillId={currentTaskData.listeningId}
                />
              ) : (
                <div className={styles.noQuestionBox}>
                  <h3>No Questions Found</h3>
                  <p>This listening test doesn't have any questions configured yet.</p>
                </div>
              )}
            </form>
          </div>

          {/* Slim audio playbar BELOW the panel */}
          <div className={styles.audioPlaybarWrap}>
            {currentTaskData?.listeningContent ? (
              <audio controls className={styles.audioPlaybar}>
                <source src={currentTaskData.listeningContent} type="audio/mpeg" />
                Your browser does not support the audio element.
              </audio>
            ) : (
              <div className={styles.noAudioSmall}>No audio available for this part.</div>
            )}
          </div>
        </div>
      </div>

      {/* Bottom Navigation */}
      <div className={styles.bottomNavigation}>
        <div className={styles.navScrollContainer}>
          {(tasks || []).map((task, taskIndex) => {
            const count = getQuestionCount(task.listeningQuestion);
            return (
              <div key={task.listeningId} className={styles.navSection}>
                <div
                  className={`${styles.navSectionTitle} ${
                    currentTask === taskIndex ? styles.navSectionTitleActive : ""
                  }`}
                  onClick={() => setCurrentTask(taskIndex)}
                  role="button"
                >
                  Part {taskIndex + 1}
                </div>
                <div className={styles.navQuestions}>
                  {Array.from({ length: count }, (_, qIndex) => {
                    const qNum = qIndex + 1;
                    const answered = isAnswered(task.listeningId, qNum);
                    const isCurrentPart = currentTask === taskIndex;
                    return (
                      <button
                        type="button"
                        key={`${taskIndex}-${qIndex}`}
                        className={[
                          styles.navButton,
                          answered ? styles.completedNavButton : styles.unansweredNavButton,
                          isCurrentPart && qIndex === 0 ? styles.activeNavButton : "",
                        ].join(" ")}
                        title={answered ? "Answered" : "Unanswered"}
                        onClick={() => setCurrentTask(taskIndex)}
                      >
                        {qNum}
                      </button>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>

        <button className={styles.completeButton} onClick={handleSubmit} disabled={isSubmitting}>
          {isSubmitting ? "Submitting..." : "Complete"}
        </button>
      </div>
    </div>
  );
}
