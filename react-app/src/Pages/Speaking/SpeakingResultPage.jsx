import React, { useEffect, useState, useMemo } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as SpeakingApi from "../../Services/SpeakingApi";
import styles from "./SpeakingResultPage.module.css";

import { Volume2, Loader2, ArrowLeft } from "lucide-react";

export default function SpeakingResultPage() {
  const location = useLocation();
  const navigate = useNavigate();

  const examId = location.state?.examId;
  const examName = location.state?.examName || "Speaking Result";

  const [loading, setLoading] = useState(true);
  const [feedback, setFeedback] = useState(null);
  const [error, setError] = useState("");

  const [speakingMap, setSpeakingMap] = useState({});

  const user = JSON.parse(localStorage.getItem("user"));
  const userId = user?.userId;

  // ============================
  // LOAD SPEAKING QUESTIONS → TO GET PART 1 / PART 2 / PART 3
  // ============================
  useEffect(() => {
    if (!examId) return;
    SpeakingApi.getByExam(examId).then((list) => {
      const map = {};
      list.forEach((s) => {
        map[s.speakingId] = s.speakingType;
      });
      setSpeakingMap(map);
    });
  }, [examId]);

  // ============================
  // LOAD FEEDBACK BY EXAM
  // ============================
  useEffect(() => {
    if (!examId || !userId) {
      setError("Invalid exam or user.");
      setLoading(false);
      return;
    }

    SpeakingApi.getFeedback(examId, userId)
      .then((res) => {
        setFeedback(res);
      })
      .catch(() => {
        setError("Failed to load speaking feedback.");
      })
      .finally(() => setLoading(false));
  }, [examId, userId]);

  const playAudio = (url) => {
    if (!url) return;
    new Audio(url).play();
  };

  // ============================
  // GROUP FEEDBACK BY PART
  // ============================
  const groupedFeedback = useMemo(() => {
    if (!feedback?.feedbacks || !speakingMap) return {};

    const groups = {
      "Part 1": [],
      "Part 2": [],
      "Part 3": [],
    };

    feedback.feedbacks.forEach((f) => {
      const part = speakingMap[f.speakingId] || "Other";
      if (!groups[part]) groups[part] = [];
      groups[part].push(f);
    });

    return groups;
  }, [feedback, speakingMap]);

  // ============================
  // LOADING UI
  // ============================
  if (loading) {
    return (
      <AppLayout title="Speaking Result" sidebar={<GeneralSidebar />}>
        <div className={styles.loading}>
          <Loader2 className="animate-spin" size={28} />
          <p>Loading your speaking result…</p>
        </div>
      </AppLayout>
    );
  }

  // ============================
  // ERROR UI
  // ============================
  if (error || !feedback) {
    return (
      <AppLayout title="Speaking Result" sidebar={<GeneralSidebar />}>
        <div className={styles.errorBox}>
          <p>{error || "No result available."}</p>
          <button className={styles.backBtn} onClick={() => navigate(-1)}>
            <ArrowLeft size={16} /> Go Back
          </button>
        </div>
      </AppLayout>
    );
  }

  const finalOverall = feedback.finalOverall ?? feedback.averageOverall ?? "-";

  // ============================
  // MAIN RESULT UI
  // ============================
  return (
    <AppLayout title="Speaking Result" sidebar={<GeneralSidebar />}>
      <div className={styles.container}>
        {/* HEADER */}
        <div className={styles.header}>
          <button className={styles.backBtn} onClick={() => navigate(-1)}>
            <ArrowLeft size={18} /> Back
          </button>

          <h2>Speaking Result – {examName}</h2>

          <div className={styles.overallScore}>
            <span>Overall Band</span>
            <strong>{finalOverall}</strong>
          </div>
        </div>

        {/* RESULT BY PART */}
        <div className={styles.taskList}>
          {Object.keys(groupedFeedback).map(
            (partName) =>
              groupedFeedback[partName].length > 0 && (
                <div key={partName} className={styles.partBlock}>
                  <h2 className={styles.partTitle}>{partName}</h2>

                  {groupedFeedback[partName].map((f, index) => {
                    const ai =
                      typeof f.aiAnalysisJson === "string"
                        ? JSON.parse(f.aiAnalysisJson || "{}")
                        : f.aiAnalysisJson;

                    return (
                      <div key={index} className={styles.taskCard}>
                        <h3 className={styles.questionTitle}>
                          Question {index + 1}
                        </h3>

                        {/* AUDIO */}
                        <div className={styles.audioRow}>
                          <div className={styles.audioPlayer}>
                            <Volume2 size={18} color="#2563eb" />
                            <audio controls src={f.audioUrl} />
                          </div>
                        </div>

                        {/* TRANSCRIPT */}
                        <div className={styles.transcriptBox}>
                          <h4>Transcript</h4>
                          <p>
                            {f.transcript || "Transcript is not available."}
                          </p>
                        </div>

                        {/* SCORES */}
                        <div className={styles.scoreGrid}>
                          <div
                            className={
                              f.pronunciation >= 7
                                ? styles.scoreHigh
                                : f.pronunciation >= 5
                                ? styles.scoreMid
                                : styles.scoreLow
                            }
                          >
                            <strong>Pronunciation:</strong>
                            <span>{f.pronunciation ?? "-"}</span>
                          </div>

                          <div
                            className={
                              f.fluency >= 7
                                ? styles.scoreHigh
                                : f.fluency >= 5
                                ? styles.scoreMid
                                : styles.scoreLow
                            }
                          >
                            <strong>Fluency:</strong>
                            <span>{f.fluency ?? "-"}</span>
                          </div>

                          <div
                            className={
                              f.lexicalResource >= 7
                                ? styles.scoreHigh
                                : f.lexicalResource >= 5
                                ? styles.scoreMid
                                : styles.scoreLow
                            }
                          >
                            <strong>Lexical Resource:</strong>
                            <span>{f.lexicalResource ?? "-"}</span>
                          </div>

                          <div
                            className={
                              f.grammarAccuracy >= 7
                                ? styles.scoreHigh
                                : f.grammarAccuracy >= 5
                                ? styles.scoreMid
                                : styles.scoreLow
                            }
                          >
                            <strong>Grammar Accuracy:</strong>
                            <span>{f.grammarAccuracy ?? "-"}</span>
                          </div>

                          <div
                            className={
                              f.coherence >= 7
                                ? styles.scoreHigh
                                : f.coherence >= 5
                                ? styles.scoreMid
                                : styles.scoreLow
                            }
                          >
                            <strong>Coherence:</strong>
                            <span>{f.coherence ?? "-"}</span>
                          </div>

                          <div
                            className={
                              f.overall >= 7
                                ? styles.scoreHigh
                                : f.overall >= 5
                                ? styles.scoreMid
                                : styles.scoreLow
                            }
                          >
                            <strong>Overall:</strong>
                            <span>{f.overall ?? "-"}</span>
                          </div>
                        </div>

                        {/* AI ANALYSIS */}
                        {ai?.ai_analysis && (
                          <div className={styles.aiBox}>
                            <h4>AI Assessment</h4>

                            {ai.ai_analysis.overview && (
                              <p className={styles.overview}>
                                {ai.ai_analysis.overview}
                              </p>
                            )}

                            {ai.ai_analysis.strengths?.length > 0 && (
                              <div className={styles.listBox}>
                                <h5>Strengths</h5>
                                <ul>
                                  {ai.ai_analysis.strengths.map((s, i) => (
                                    <li key={i}>{s}</li>
                                  ))}
                                </ul>
                              </div>
                            )}

                            {ai.ai_analysis.weaknesses?.length > 0 && (
                              <div className={styles.listBox}>
                                <h5>Weaknesses</h5>
                                <ul>
                                  {ai.ai_analysis.weaknesses.map((s, i) => (
                                    <li key={i}>{s}</li>
                                  ))}
                                </ul>
                              </div>
                            )}

                            {ai.ai_analysis.advice && (
                              <div className={styles.adviceBox}>
                                <h5>Advice</h5>
                                <p>{ai.ai_analysis.advice}</p>
                              </div>
                            )}

                            {ai.ai_analysis.vocabulary_suggestions?.length >
                              0 && (
                              <div className={styles.vocabBox}>
                                <h5>Vocabulary Suggestions</h5>

                                {ai.ai_analysis.vocabulary_suggestions.map(
                                  (v, i) => (
                                    <div key={i} className={styles.vocabItem}>
                                      <strong>{v.original_word}</strong> →{" "}
                                      <span>{v.suggested_alternative}</span>
                                      <p className={styles.explain}>
                                        {v.explanation}
                                      </p>
                                    </div>
                                  )
                                )}
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )
          )}
        </div>
      </div>
    </AppLayout>
  );
}
