import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as WritingApi from "../../Services/WritingApi";
import styles from "./WritingResultPage.module.css";
import { SpellCheck, CheckCircle2, ListTree, Link2 } from "lucide-react";
function ErrorPopup({ error, isOpen, onClose, position }) {
  if (!isOpen || !error) return null;
  return (
    <div
      className={styles.errorPopup}
      style={{ ...position }}
      onClick={(e) => e.stopPropagation()}
    >
      <div className={styles.errorPopupContent}>
        <button className={styles.errorCloseBtn} onClick={onClose}>
          ✕
        </button>
        <div className={styles.errorType}>
          {error.type} • {error.category}
        </div>
        <div className={styles.errorText}>
          <div className={styles.errorIncorrect}>
            <strong>Incorrect:</strong> {error.incorrect}
          </div>
          <div className={styles.errorSuggestion}>
            <strong>Suggested:</strong> {error.suggestion}
          </div>
          <div className={styles.errorExplanation}>
            <strong>Explanation:</strong> {error.explanation}
          </div>
        </div>
      </div>
    </div>
  );
}

function TextWithErrors({ text, errors, onErrorClick, errorType = "grammarVocab" }) {
  if (!errors || errors.length === 0)
    return <div className={styles.essayText}>{text}</div>;

  let processedText = text || "";
  const parts = [];
  let lastIndex = 0;

  errors.forEach((error, index) => {
    const errTxt = error.incorrect;
    const pos = processedText.toLowerCase().indexOf(errTxt.toLowerCase(), lastIndex);
    if (pos !== -1) {
      if (pos > lastIndex) {
        parts.push({
          type: "text",
          content: processedText.substring(lastIndex, pos),
          key: `text-${lastIndex}`,
        });
      }
      parts.push({
        type: "error",
        content: processedText.substring(pos, pos + errTxt.length),
        error,
        key: `error-${index}`,
      });
      lastIndex = pos + errTxt.length;
    }
  });

  if (lastIndex < processedText.length) {
    parts.push({
      type: "text",
      content: processedText.substring(lastIndex),
      key: `text-${lastIndex}`,
    });
  }

  return (
    <div className={styles.essayText}>
      {parts.map((p) =>
        p.type === "error" ? (
          <span
            key={p.key}
            className={`${styles.errorHighlight} ${
              errorType === "coherenceLogic"
                ? styles.coherenceLogicError
                : styles.grammarVocabError
            }`}
            onClick={(e) => onErrorClick(e, p.error)}
          >
            {p.content}
          </span>
        ) : (
          <span key={p.key}>{p.content}</span>
        )
      )}
    </div>
  );
}
export default function WritingResultPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const [selectedError, setSelectedError] = useState(null);
  const [errorPopupPosition, setErrorPopupPosition] = useState({ top: 0, left: 0 });
  const [isLoading, setIsLoading] = useState(true);
  const [progress, setProgress] = useState(0);
  const [feedbackData, setFeedbackData] = useState(null);

  if (!state) {
    return (
      <div className={styles.center}>
        <h2>No result data found</h2>
        <button onClick={() => navigate("/")} className={styles.backBtn}>
          ← Back
        </button>
      </div>
    );
  }
  const { examId, userId, exam, mode, originalAnswers, isWaiting } = state;
  useEffect(() => {
    let interval;
    let progressTimer;

    const fetchFeedback = async () => {
      try {
        const res = await WritingApi.getFeedback(examId, userId);
        if (res?.feedbacks?.length > 0) {
          setFeedbackData(res);
          setIsLoading(false);
          clearInterval(interval);
          clearInterval(progressTimer);
        }
      } catch {}
    };

    if (isWaiting) {
      setProgress(0);
      progressTimer = setInterval(() => {
        setProgress((p) => (p < 90 ? p + 5 : p));
      }, 1000);
      interval = setInterval(fetchFeedback, 2500);
      fetchFeedback();
    } else {
      setIsLoading(false);
      setFeedbackData({
        feedbacks: state.feedbacks,
        averageOverall: state.averageBand,
      });
    }
    return () => {
      clearInterval(interval);
      clearInterval(progressTimer);
    };
  }, [examId, userId, isWaiting, state]);

  const handleErrorClick = (event, error) => {
    const rect = event.target.getBoundingClientRect();
    setErrorPopupPosition({ top: rect.bottom + 5, left: rect.left });
    setSelectedError(error);
  };
  if (isLoading) {
    return (
      <div className={styles.loadingContainer}>
        <div className={styles.loadingSpinner}></div>
        <p className={styles.loadingText}>
          AI is grading your essay... Please wait.
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
  const { feedbacks, averageOverall } = feedbackData || { feedbacks: [], averageOverall: 0 };
  const filteredFeedbacks = feedbacks.filter(
    (f) =>
      !originalAnswers ||
      Object.keys(originalAnswers).includes(String(f.writingId))
  );

  return (
    <div className={styles.pageLayout}>
      <GeneralSidebar />
      <div className={styles.mainContent}>
        <div className={styles.header}>
          <h2>
            Writing Test Result — {exam?.examName || "Unknown Exam"} ({mode})
          </h2>
        </div>

        {filteredFeedbacks.map((f, i) => {
          const originalText =
            originalAnswers?.[f.writingId] || f.answerText || "";

          const grammarVocabParsed = JSON.parse(f.grammarVocabJson || "{}");
          const coherenceLogicParsed = JSON.parse(f.feedbackSections || "{}");

          return (
            <div key={i} className={styles.resultContainer}>
              {/* ===== LEFT PANEL ===== */}
              <div className={styles.leftPanel}>
                <div className={styles.criteriaHeader}>
            
                </div>

                <div className={styles.feedbackSection}>
                  <h3 className={styles.sectionTitle}>Grammar & Vocabulary</h3>
                  {grammarVocabParsed.overview && (
                    <div className={styles.overviewBox}>
                      <strong>Overview:</strong> {grammarVocabParsed.overview}
                    </div>
                  )}
                  <div className={styles.correctedTextBox}>
                    <TextWithErrors
                      text={originalText}
                      errors={grammarVocabParsed.errors || []}
                      onErrorClick={handleErrorClick}
                      errorType="grammarVocab"
                    />
                  </div>
                </div>
              </div>

              {/* ===== RIGHT PANEL ===== */}
              <div className={styles.rightPanel}>
                <div className={styles.criteriaHeader}>
                  <div className={styles.squareGrid}>
                  
                  </div>
                </div>

                <div className={styles.bandScoreBox}>
                  <div className={styles.bandOverall}>
                    <h4>Band Score</h4>
                    <div className={styles.bandMain}>{f.overall}</div>
                  </div>
                  <div className={styles.bandGrid}>
                    <div>
                      <b>Task Achievement</b>
                      <p>{f.taskAchievement}</p>
                    </div>
                    <div>
                      <b>Coherence & Cohesion</b>
                      <p>{f.coherenceCohesion}</p>
                    </div>
                    <div>
                      <b>Lexical Resource</b>
                      <p>{f.lexicalResource}</p>
                    </div>
                    <div>
                      <b>Grammar Accuracy</b>
                      <p>{f.grammarAccuracy}</p>
                    </div>
                  </div>
                </div>
                <div className={styles.feedbackSection}>
                  <h3 className={styles.sectionTitle}>Coherence & Cohesion</h3>
                  {coherenceLogicParsed.overview && (
                    <div className={styles.overviewBox}>
                      <strong>Overview:</strong> {coherenceLogicParsed.overview}
                    </div>
                  )}
                </div>
              </div>
            </div>
          );
        })}
        <ErrorPopup
          error={selectedError}
          isOpen={!!selectedError}
          onClose={() => setSelectedError(null)}
          position={errorPopupPosition}
        />
      </div>
    </div>
  );
}
