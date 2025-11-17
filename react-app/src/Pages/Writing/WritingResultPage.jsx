import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as WritingApi from "../../Services/WritingApi";
import * as ExamApi from "../../Services/ExamApi";
import styles from "./WritingResultPage.module.css";
import { SpellCheck, CheckCircle2, ListTree, Link2 } from "lucide-react";

/* ====================== Error Popup ====================== */
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

/* ====================== Highlighted Text Component ====================== */
function TextWithErrors({
  text,
  errors,
  onErrorClick,
  errorType = "grammarVocab",
  replacedTexts = {},
  writingId,
}) {
  if (!errors || errors.length === 0)
    return <div className={styles.essayText}>{text}</div>;

  let processedText = text || "";

  // Apply text replacements first
  if (replacedTexts[writingId]) {
    Object.entries(replacedTexts[writingId]).forEach(
      ([original, replacement]) => {
        processedText = processedText.replace(
          new RegExp(original.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"), "gi"),
          replacement
        );
      }
    );
  }

  const parts = [];
  let lastIndex = 0;

  errors.forEach((error, index) => {
    const errTxt = error.incorrect;
    const pos = processedText
      .toLowerCase()
      .indexOf(errTxt.toLowerCase(), lastIndex);
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
            className={`${styles.errorHighlight} ${errorType === "coherenceLogic"
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

/* ====================== MAIN PAGE ====================== */
export default function WritingResultPage() {
  const { state } = useLocation();
  const navigate = useNavigate();

  const [selectedError, setSelectedError] = useState(null);
  const [errorPopupPosition, setErrorPopupPosition] = useState({
    top: 0,
    left: 0,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [progress, setProgress] = useState(0);
  const [feedbackData, setFeedbackData] = useState(null);
  const [replacedTexts, setReplacedTexts] = useState({});
  const [activeTaskIndex, setActiveTaskIndex] = useState(0);
  const [fetchedAnswers, setFetchedAnswers] = useState(null);

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

  const { examId, userId, exam, mode, originalAnswers, isWaiting, attemptId } = state || {};

  /* ====================== Fetch AnswerText from ExamAttempt ====================== */
  useEffect(() => {
    // Chỉ fetch nếu không có originalAnswers
    if (originalAnswers || !examId || !userId) return;

    const fetchAnswers = async () => {
      try {
        // Tìm attemptId cho exam này
        let targetAttemptId = attemptId;
        if (!targetAttemptId) {
          const attempts = await ExamApi.getExamAttemptsByUser(userId);
          const attempt = attempts.find((a) => a.examId === examId);
          targetAttemptId = attempt?.attemptId;
        }

        if (targetAttemptId) {
          const attemptDetail = await ExamApi.getExamAttemptDetail(targetAttemptId);
          if (attemptDetail?.answerText) {
            let answerText = attemptDetail.answerText;

            // Loại bỏ phần feedback JSON nếu có (format: "answer\n\n---\n[AI Feedback JSON]\n...")
            const feedbackMarker = "\n\n---\n[AI Feedback JSON]\n";
            if (answerText.includes(feedbackMarker)) {
              answerText = answerText.split(feedbackMarker)[0];
            }
            const parseTaskAnswers = (rawText) => {
              const map = {};
              const blocks = rawText.split(/--- TASK (\d+) ---/g).slice(1);

              // blocks = ["10", "text1", "9", "text2", ...]
              for (let i = 0; i < blocks.length; i += 2) {
                const id = Number(blocks[i]);       // writingId
                const text = blocks[i + 1].trim();  // nội dung
                map[id] = text;
              }

              return map;
            };

            setFetchedAnswers(parseTaskAnswers(answerText));
          }
        }
      } catch (err) {
        console.warn("Failed to fetch answerText from ExamAttempt:", err);
      }
    };

    fetchAnswers();
  }, [examId, userId, originalAnswers, attemptId]);

  /* ====================== Fetch Feedback ====================== */
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
      } catch { }
    };

    if (isWaiting) {
      setProgress(0);
      progressTimer = setInterval(() => {
        setProgress((p) => (p < 90 ? p + 5 : p));
      }, 1000);
      interval = setInterval(fetchFeedback, 2500);
      fetchFeedback();
    } else {
      WritingApi.getFeedback(examId, userId).then((res) => {
        setFeedbackData(res);
        setIsLoading(false);
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

  const handleReplaceText = (originalText, improvedText, writingId) => {
    setReplacedTexts((prev) => ({
      ...prev,
      [writingId]: {
        ...prev[writingId],
        [originalText]: improvedText,
      },
    }));
  };

  const getDisplayText = (originalText, writingId) => {
    const replaced = replacedTexts[writingId]?.[originalText];
    return replaced || originalText;
  };

  /* ====================== Loading State ====================== */
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

  const { feedbacks, averageOverall } = feedbackData || {
    feedbacks: [],
    averageOverall: 0,
  };
  const filteredFeedbacks = feedbacks;

  /* ====================== RENDER ====================== */
  if (!feedbackData || filteredFeedbacks.length === 0) {
    return (
      <div className={styles.pageLayout}>
        <GeneralSidebar />
        <div className={styles.mainContent}>
          <div className={styles.header}>
            <h2>
              Writing Test Result — {exam?.examName || "Unknown Exam"} ({mode})
            </h2>
          </div>
          <div className={styles.center}>
            <h2>No feedback available</h2>
            <p>There is no feedback data for this writing test.</p>
            <button onClick={() => navigate("/dashboard")} className={styles.backBtn}>
              ← Back to Dashboard
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.pageLayout}>
      <GeneralSidebar />
      <div className={styles.mainContent}>
        <div className={styles.header}>
          <h2>
            Writing Test Result — {exam?.examName || "Unknown Exam"} ({mode})
          </h2>
        </div>

        {filteredFeedbacks.length > 1 && (
          <div className={styles.taskSwitcher}>
            {filteredFeedbacks.map((_, idx) => (
              <button
                key={idx}
                className={`${styles.taskBtn} ${idx === activeTaskIndex ? styles.taskBtnActive : ""
                  }`}
                onClick={() => setActiveTaskIndex(idx)}
              >
                {`Task ${idx + 1}`}
              </button>
            ))}
          </div>
        )}

        {filteredFeedbacks.map((f, i) => {
          if (i !== activeTaskIndex) return null;

          // Ưu tiên: originalAnswers (từ state) > fetchedAnswers (từ ExamAttempt) > f.answerText (từ feedback) > ""
          let originalText = "";
          if (originalAnswers?.[f.writingId]) {
            originalText = originalAnswers[f.writingId];
          } else if (fetchedAnswers) {
            if (fetchedAnswers[f.writingId]) {
              originalText = fetchedAnswers[f.writingId];
            } else if (fetchedAnswers._single) {
              originalText = fetchedAnswers._single;
            }
          }
          else {
            originalText = f.answerText || "";
          }

          // === parse new JSON fields ===
          const grammarVocabParsed = JSON.parse(f.grammarVocabJson || "{}");
          const overallFeedbackParsed = JSON.parse(f.feedbackSections || "{}");

          return (
            <div key={i} className={styles.resultContainer}>
              {/* ===== LEFT PANEL ===== */}
              <div className={styles.leftPanel}>
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
                      replacedTexts={replacedTexts}
                      writingId={f.writingId}
                    />
                  </div>
                </div>
              </div>

              {/* ===== RIGHT PANEL ===== */}
              <div className={styles.rightPanel}>
                {/* === Band Score Box === */}
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
                      <b>Organization & Logic</b>
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

                {/* === Overall Feedback Section === */}
                <div className={styles.feedbackSection}>
                  <h3 className={styles.sectionTitle}>Overall Feedback</h3>

                  {overallFeedbackParsed.overview && (
                    <div className={styles.overviewBox}>
                      <strong>Overview:</strong>{" "}
                      {overallFeedbackParsed.overview}
                    </div>
                  )}

                  {overallFeedbackParsed.refinements &&
                    overallFeedbackParsed.refinements.length > 0 && (
                      <div className={styles.paragraphFeedbackBox}>
                        <h4 style={{ marginBottom: "12px" }}>
                          Refined Vocabulary & Collocations
                        </h4>
                        {overallFeedbackParsed.refinements.map((r, idx) => (
                          <div key={idx} className={styles.refinementItem}>
                            <div className={styles.refinementRow}>
                              <div className={styles.refinementOld}>
                                <strong>Old:</strong>{" "}
                                <span className={styles.oldWord}>
                                  {r.original}
                                </span>
                              </div>
                              <div className={styles.refinementArrow}>→</div>
                              <div className={styles.refinementNew}>
                                <strong>New:</strong>{" "}
                                <span
                                  className={styles.newWord}
                                  onClick={() =>
                                    handleReplaceText(
                                      r.original,
                                      r.improved,
                                      f.writingId
                                    )
                                  }
                                  title="Click to replace in essay"
                                >
                                  {r.improved}
                                </span>
                              </div>
                            </div>
                            <div className={styles.refinementExplanation}>
                              <strong>Explanation:</strong> {r.explanation}
                            </div>
                          </div>
                        ))}
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
