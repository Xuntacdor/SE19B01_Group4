import React, { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as ReadingApi from "../../Services/ReadingApi";
import * as ExamApi from "../../Services/ExamApi";
import styles from "./ReadingResultPage.module.css";
import ExamMarkdownRenderer from "../../Components/Exam/ExamMarkdownRenderer";

export default function ReadingResultPage() {
  const { state } = useLocation();
  const navigate = useNavigate();

  const [attempt, setAttempt] = useState(null);
  const [readings, setReadings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [progress, setProgress] = useState(0);

  if (!state) {
    return (
      <div className={styles.center}>
        <h2>No result data found</h2>
        <button onClick={() => navigate("/reading")} className={styles.backBtn}>
          ← Back
        </button>
      </div>
    );
  }

  const { attemptId, examName, isWaiting } = state;

  // ===== Fetch Attempt =====
  useEffect(() => {
    let timer;
    const fetchAttempt = async () => {
      try {
        const res = await ExamApi.getExamAttemptDetail(attemptId);
        if (res) {
          setAttempt(res);
          setLoading(false);
          clearInterval(timer);
        }
      } catch (err) {
        console.error("❌ Failed to fetch attempt:", err);
      }
    };

    if (isWaiting) {
      timer = setInterval(() => setProgress((p) => (p < 90 ? p + 5 : p)), 1000);
    }

    fetchAttempt();
    return () => clearInterval(timer);
  }, [attemptId, isWaiting]);

  // ===== Fetch Readings =====
  useEffect(() => {
    if (!attempt?.examId) return;
    (async () => {
      try {
        const res = await ReadingApi.getByExam(attempt.examId);
        setReadings(res || []);
      } catch (err) {
        console.error("Failed to load readings:", err);
      }
    })();
  }, [attempt]);

  if (loading) {
    return (
      <div className={styles.center}>
        <div className={styles.loadingSpinner}></div>
        <p className={styles.loadingText}>
          Evaluating your reading attempt... Please wait.
        </p>
        <div className={styles.progressBar}>
          <div
            className={styles.progressFill}
            style={{ width: `${progress}%` }}
          ></div>
        </div>
      </div>
    );
  }

  if (!attempt) {
    return (
      <div className={styles.center}>
        <h3>No attempt found.</h3>
        <button onClick={() => navigate("/reading")} className={styles.backBtn}>
          ← Back
        </button>
      </div>
    );
  }

  // ===== Parse user's answers =====
  const parsedAnswers = (() => {
    try {
      return JSON.parse(attempt.answerText || "[]");
    } catch {
      return [];
    }
  })();

  // Build map: readingId → userAnswers
  const userAnswerMap = new Map();
  parsedAnswers.forEach((g) => {
    if (g.SkillId && g.Answers) {
      userAnswerMap.set(g.SkillId, g.Answers);
    }
  });

  // 🟢 NEW: Detect full vs partial attempt
  const attemptedSkillIds = [...userAnswerMap.keys()];
  const totalSkills = readings.length;
  const attemptedSkills = attemptedSkillIds.length;
  const isFullExam = totalSkills > 0 && attemptedSkills === totalSkills;

  // ===== Compute totals =====
  let totalQuestions = 0;
  let correctCount = 0;

  const normalize = (val) => {
    if (Array.isArray(val)) return val.map((x) => x.trim().toLowerCase());
    if (typeof val === "string") return [val.trim().toLowerCase()];
    return [];
  };

  // 🟢 Only evaluate readings that were actually attempted
  const readingsWithCorrect = readings
    .filter((r) => attemptedSkillIds.includes(r.readingId))
    .map((r) => {
      let correctAnswers = {};
      try {
        correctAnswers = JSON.parse(r.correctAnswer || "{}");
      } catch {
        correctAnswers = {};
      }

      const userAnswers = userAnswerMap.get(r.readingId) || {};
      const correctKeys = Object.keys(correctAnswers);

      // 🟢 Count total correct options instead of questions
      correctKeys.forEach((key) => {
        const userVal = normalize(userAnswers[key] || []);
        const correctVal = normalize(correctAnswers[key] || []);

        // Each correct option counts individually
        totalQuestions += correctVal.length;

        // Count how many of those the user got right
        correctVal.forEach((opt) => {
          if (userVal.map((v) => v.toLowerCase()).includes(opt.toLowerCase())) {
            correctCount++;
          }
        });
      });
      
      return { ...r, correctAnswers, userAnswers };
    });

  const accuracy =
    totalQuestions > 0 ? (correctCount / totalQuestions) * 100 : 0;

  // ===== Render =====
  return (
    <div className={styles.pageLayout}>
      <GeneralSidebar />
      <div className={styles.mainContent}>
        {/* ===== Header ===== */}
        <div className={styles.header}>
          <h2>Reading Result — {examName || attempt.examName}</h2>
          {!isFullExam && (
            <p className={styles.partialNotice}>
              ⚠️ You completed only {attemptedSkills}/{totalSkills} part(s) of
              this exam. The score shown below reflects a partial attempt.
            </p>
          )}
        </div>

        {/* ===== Band Summary ===== */}
        <div className={styles.bandScoreBox}>
          <div className={styles.bandOverall}>
            <h4>IELTS Band</h4>
            <div className={styles.bandMain}>
              {attempt.totalScore?.toFixed(1) || "-"}
            </div>
          </div>
          <div className={styles.bandGrid}>
            <div>
              <b>Correct</b>
              <p>{correctCount}</p>
            </div>
            <div>
              <b>Total</b>
              <p>{totalQuestions}</p>
            </div>
            <div>
              <b>Accuracy</b>
              <p>{accuracy.toFixed(1)}%</p>
            </div>
          </div>
        </div>

        {/* ===== Embedded Correction ===== */}
        {readingsWithCorrect
          .filter((r) => attemptedSkillIds.includes(r.readingId)) // 🟢 NEW: show only attempted parts
          .map((r, i) => (
            <div key={i} className={styles.resultContainer}>
              <div className={styles.leftPanel}>
                <h3>Passage {r.displayOrder || i + 1}</h3>
                <div
                  className={styles.passageContent}
                  dangerouslySetInnerHTML={{
                    __html: r.readingContent || "<i>No passage content</i>",
                  }}
                />
              </div>

              <div className={styles.rightPanel}>
                <h3>Questions & Answers</h3>
                <ExamMarkdownRenderer
                  markdown={r.readingQuestion}
                  showAnswers={true}
                  userAnswers={[
                    { SkillId: r.readingId, Answers: r.userAnswers },
                  ]}
                  correctAnswers={[
                    { SkillId: r.readingId, Answers: r.correctAnswers },
                  ]}
                  readingId={r.readingId}
                />
              </div>
            </div>
          ))}
        <div className={styles.footer}>
          <button
            className={styles.backBtn}
            onClick={() => navigate("/reading")}
          >
            ← Back to Reading List
          </button>
        </div>
      </div>
    </div>
  );
}
