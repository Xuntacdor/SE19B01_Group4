import React, { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as ListeningApi from "../../Services/ListeningApi";
import * as ExamApi from "../../Services/ExamApi";
import ExamMarkdownRenderer from "../../Components/Exam/ExamMarkdownRenderer";
import styles from "./ListeningResultPage.module.css";
import { Headphones } from "lucide-react";

export default function ListeningResultPage() {
  const { state } = useLocation();
  const navigate = useNavigate();

  const [attempt, setAttempt] = useState(null);
  const [listenings, setListenings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [progress, setProgress] = useState(0);

  if (!state) {
    return (
      <div className={styles.center}>
        <h2>No result data found</h2>
        <button onClick={() => navigate("/listening")} className={styles.backBtn}>
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
      timer = setInterval(
        () => setProgress((p) => (p < 90 ? p + 5 : p)),
        1000
      );
    }

    fetchAttempt();
    return () => clearInterval(timer);
  }, [attemptId, isWaiting]);

  // ===== Fetch Listening Parts =====
  useEffect(() => {
    if (!attempt?.examId) return;
    (async () => {
      try {
        const res = await ListeningApi.getByExam(attempt.examId);
        setListenings(res || []);
      } catch (err) {
        console.error("Failed to load listening parts:", err);
      }
    })();
  }, [attempt]);

  if (loading) {
    return (
      <div className={styles.center}>
        <div className={styles.loadingSpinner}></div>
        <p className={styles.loadingText}>
          Evaluating your listening attempt... Please wait.
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
        <button onClick={() => navigate("/listening")} className={styles.backBtn}>
          ← Back
        </button>
      </div>
    );
  }

  // ===== Parse answers =====
  const parsedAnswers = (() => {
    try {
      return JSON.parse(attempt.answerText || "[]");
    } catch {
      return [];
    }
  })();

  // Map answers by skill
  const answerMap = new Map();
  parsedAnswers.forEach((g) => {
    if (g.SkillId && Array.isArray(g.Answers)) {
      answerMap.set(g.SkillId, g.Answers);
    }
  });

  const attemptedSkillIds = parsedAnswers.map((g) => g.SkillId);
  const relevantListenings = listenings.filter((l) =>
    attemptedSkillIds.includes(l.listeningId)
  );

  // ===== Score calculation =====
  let totalSubQuestions = 0;
  let correctCount = 0;

  relevantListenings.forEach((l) => {
    const userAnswers = answerMap.get(l.listeningId) || [];
    let correctAnswers = [];
    try {
      if (l.correctAnswer?.trim().startsWith("[")) {
        correctAnswers = JSON.parse(l.correctAnswer);
      } else {
        correctAnswers = l.correctAnswer
          ? l.correctAnswer.split(/[,;]+/).map((x) => x.trim())
          : [];
      }
    } catch {
      correctAnswers = [];
    }

    totalSubQuestions += correctAnswers.length;

    correctAnswers.forEach((ans, i) => {
      const userAns = userAnswers[i] || "_";
      if (userAns.trim().toLowerCase() === ans.trim().toLowerCase()) {
        correctCount++;
      }
    });
  });

  const accuracy =
    totalSubQuestions > 0 ? (correctCount / totalSubQuestions) * 100 : 0;

  // ===== Render =====
  return (
    <div className={styles.pageLayout}>
      <GeneralSidebar />
      <div className={styles.mainContent}>
        {/* ===== Header ===== */}
        <div className={styles.header}>
          <h2>
            <Headphones size={22} style={{ marginRight: 8 }} />
            Listening Result — {examName || attempt.examName}
          </h2>
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
              <p>{totalSubQuestions}</p>
            </div>
            <div>
              <b>Accuracy</b>
              <p>{accuracy.toFixed(1)}%</p>
            </div>
          </div>
        </div>

        {/* ===== Listening Parts ===== */}
        {relevantListenings.map((l, i) => {
          const userAnswers = answerMap.get(l.listeningId) || [];
          let correctAnswers = [];
          try {
            correctAnswers = JSON.parse(l.correctAnswer || "[]");
          } catch {
            correctAnswers = [];
          }

          return (
            <div key={i} className={styles.resultContainer}>
              <div className={styles.leftPanel}>
                <h3>Part {l.displayOrder || i + 1}</h3>
                {l.audioUrl ? (
                  <audio controls className={styles.audioPlayer}>
                    <source src={l.audioUrl} type="audio/mpeg" />
                    Your browser does not support the audio element.
                  </audio>
                ) : (
                  <p className={styles.noAudio}>No audio available</p>
                )}
                {l.listeningTranscript && (
                  <div className={styles.transcriptBox}>
                    <h4>Transcript</h4>
                    <div
                      dangerouslySetInnerHTML={{
                        __html: l.listeningTranscript,
                      }}
                    />
                  </div>
                )}
              </div>

              <div className={styles.rightPanel}>
                <h3>Questions & Answers</h3>
                <ExamMarkdownRenderer
                  markdown={l.listeningQuestion}
                  showAnswers={true}
                  userAnswers={userAnswers}
                  correctAnswers={correctAnswers}
                />
              </div>
            </div>
          );
        })}

        <div className={styles.footer}>
          <button className={styles.backBtn} onClick={() => navigate("/listening")}>
            ← Back to Listening List
          </button>
        </div>
      </div>
    </div>
  );
}
