import React, { useEffect, useState, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { submitAttempt } from "../../Services/ExamApi";
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

  const handleAnswerChange = (questionNumber, value) => {
    setAnswers(prev => ({
      ...prev,
      [questionNumber]: value
    }));
  };

  const handleQuestionNavigation = (questionNumber) => {
    setCurrentTask(questionNumber - 1);
  };

  // 📝 Submission logic
  const handleSubmit = (e) => {
    e?.preventDefault();
    if (isSubmitting) return;
    setIsSubmitting(true);

    const structuredAnswers = tasks.map((task, index) => {
      const taskAnswers = [];
      // Extract answers for this task
      Object.keys(answers).forEach(key => {
        if (key.startsWith(`${task.readingId}_`)) {
          taskAnswers.push(answers[key]);
        }
      });
      return { skillId: task.readingId, answers: taskAnswers };
    });

    const answerText = JSON.stringify(structuredAnswers);
    const attempt = {
      examId: exam.examId,
      answerText,
      startedAt: new Date().toISOString(),
    };

    submitAttempt(attempt)
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

  // Get question count for navigation - find actual question numbers
  const getQuestionCount = (readingQuestion) => {
    if (!readingQuestion) return 0;
    
    // Look for question numbers that are likely to be actual questions
    // Pattern 1: Numbers at the end of lines (like "9", "10", "11" at end of fill-in questions)
    const endOfLineNumbers = readingQuestion.match(/(\d+)\s*$/gm);
    
    // Pattern 2: Numbers after [!num] markers
    const numMarkerNumbers = readingQuestion.match(/\[!num\]\s*(\d+)/g);
    
    // Pattern 3: Numbers in question ranges like "Questions 9 - 13"
    const rangeNumbers = readingQuestion.match(/Questions?\s+(\d+)\s*-\s*(\d+)/gi);
    
    let allQuestionNumbers = [];
    
    if (endOfLineNumbers) {
      allQuestionNumbers.push(...endOfLineNumbers.map(Number));
    }
    
    if (numMarkerNumbers) {
      allQuestionNumbers.push(...numMarkerNumbers.map(match => {
        const num = match.match(/(\d+)/);
        return num ? Number(num[1]) : 0;
      }));
    }
    
    if (rangeNumbers) {
      rangeNumbers.forEach(range => {
        const match = range.match(/(\d+)\s*-\s*(\d+)/i);
        if (match) {
          const start = Number(match[1]);
          const end = Number(match[2]);
          for (let i = start; i <= end; i++) {
            allQuestionNumbers.push(i);
          }
        }
      });
    }
    
    // Filter out unreasonable numbers (keep only 1-50 range for questions)
    const validQuestionNumbers = allQuestionNumbers.filter(n => n >= 1 && n <= 50);
    const maxQuestionNumber = validQuestionNumbers.length > 0 ? Math.max(...validQuestionNumbers) : 0;
    
    console.log('All numbers found:', allQuestionNumbers);
    console.log('Valid question numbers:', validQuestionNumbers);
    console.log('Max question number:', maxQuestionNumber);
    
    return maxQuestionNumber;
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

  const currentTaskData = tasks[currentTask];
  const questionCount = getQuestionCount(currentTaskData?.readingQuestion);
  
  // Debug logging
  console.log('Current task data:', currentTaskData);
  console.log('Reading question markdown:', currentTaskData?.readingQuestion);
  console.log('Question count:', questionCount);
  console.log('All tasks:', tasks);

  return (
    <div className={styles.examWrapper}>
      {/* Top Header */}
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
        {/* Left Panel - Reading Passage */}
        <div className={styles.leftPanel}>
          {/* Passage Header - Only show if we have passage metadata */}
          {currentTaskData?.passageTitle && (
            <div className={styles.passageHeader}>
              <h3 className={styles.passageTitle}>
                {currentTaskData.passageTitle}
            </h3>
            </div>
          )}

          {/* Passage Content */}
          <div className={styles.passageContent}>
            <div
              dangerouslySetInnerHTML={{ __html: currentTaskData?.readingContent || "" }}
            />
          </div>
        </div>

        {/* Right Panel - Questions */}
        <div className={styles.rightPanel}>
          <form ref={formRef}>
            {currentTaskData?.readingQuestion ? (
              <ExamMarkdownRenderer 
                markdown={currentTaskData.readingQuestion}
                showAnswers={false}
              />
            ) : (
              <div className={styles.questionSection}>
                <div style={{ padding: '40px', textAlign: 'center', color: '#666' }}>
                  <h3>No Questions Found</h3>
                  <p>This reading test doesn't have any questions configured yet.</p>
                  <p>Please check the reading content format or contact the administrator.</p>
                </div>
              </div>
            )}
          </form>
        </div>
      </div>

      {/* Bottom Navigation */}
      <div className={styles.bottomNavigation}>
        {/* Show navigation for all questions */}
        {Array.from({ length: questionCount }, (_, i) => i + 1).map((num) => (
          <button
            key={num}
            className={`${styles.navButton} ${
              currentTask === num - 1 ? styles.activeNavButton : ''
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
