import React, { useState, useEffect, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import AppLayout from "../../Components/Layout/AppLayout";
import * as SpeakingApi from "../../Services/SpeakingApi";
import {
  Mic,
  Volume2,
  Play,
  Pause,
  Trophy,
  TrendingUp,
  Target,
  Clock,
  CheckCircle,
  AlertTriangle,
  RotateCcw,
  Download,
  Share2,
} from "lucide-react";
import styles from "./SpeakingResultPage.module.css";

// --- AudioPlayer Component ---
function AudioPlayer({ audioUrl, transcript, onPlay, onPause, isPlaying }) {
  return (
    <div className={styles.audioPlayer}>
      <div className={styles.audioHeader}>
        <Volume2 size={20} />
        <span>Your Recording</span>
      </div>
      <div className={styles.audioControls}>
        <button
          className={styles.playButton}
          onClick={isPlaying ? onPause : onPlay}
        >
          {isPlaying ? <Pause size={16} /> : <Play size={16} />}
        </button>
        <div className={styles.audioInfo}>
          <span>Click to {isPlaying ? "pause" : "play"} your recording</span>
        </div>
      </div>

      {transcript &&
        transcript.trim() !== "" &&
        transcript !== "[Transcription failed]" && (
          <div className={styles.transcriptSection}>
            <h4>Transcript:</h4>
            <p>{transcript}</p>
          </div>
        )}
      {transcript === "[Transcription failed]" && (
        <div className={styles.transcriptSection}>
          <p style={{ color: "red" }}>
            Transcription failed for this recording.
          </p>
        </div>
      )}
    </div>
  );
}

// --- ScoreCard Component ---
function ScoreCard({ title, score, maxScore = 9, color, icon: Icon }) {
  const percentage = (score / maxScore) * 100;
  const scoreText =
    score >= 7 ? "Excellent" : score >= 5 ? "Good" : "Needs Improvement";

  return (
    <div className={styles.scoreCard}>
      <div className={styles.scoreHeader}>
        <Icon size={20} style={{ color }} />
        <h4>{title}</h4>
      </div>
      <div className={styles.scoreValue}>
        <span className={styles.scoreNumber}>{score}</span>
        <span className={styles.scoreMax}>/ {maxScore}</span>
      </div>
      <div className={styles.scoreBar}>
        <div
          className={styles.scoreFill}
          style={{
            width: `${percentage}%`,
            backgroundColor: color,
          }}
        ></div>
      </div>
      <div className={styles.scoreLabel}>{scoreText}</div>
    </div>
  );
}

// --- FeedbackSection Component ---
function FeedbackSection({ feedback, title, icon: Icon }) {
  const [isExpanded, setIsExpanded] = useState(true);
  if (!feedback) return null;

  return (
    <div className={styles.feedbackSection}>
      <div
        className={styles.feedbackHeader}
        onClick={() => setIsExpanded(!isExpanded)}
      >
        <Icon size={20} />
        <h3>{title}</h3>
        <span className={styles.expandIcon}>{isExpanded ? "−" : "+"}</span>
      </div>
      {isExpanded && (
        <div className={styles.feedbackContent}>
          {feedback.overview && (
            <div className={styles.overview}>
              <h4>Overview</h4>
              <p>{feedback.overview}</p>
            </div>
          )}
          {feedback.strengths?.length > 0 && (
            <div className={styles.strengths}>
              <h4>Strengths</h4>
              <ul>
                {feedback.strengths.map((s, i) => (
                  <li key={i}>
                    <CheckCircle size={14} /> {s}
                  </li>
                ))}
              </ul>
            </div>
          )}
          {feedback.weaknesses?.length > 0 && (
            <div className={styles.weaknesses}>
              <h4>Areas for Improvement</h4>
              <ul>
                {feedback.weaknesses.map((w, i) => (
                  <li key={i}>
                    <AlertTriangle size={14} /> {w}
                  </li>
                ))}
              </ul>
            </div>
          )}
          {feedback.advice && (
            <div className={styles.advice}>
              <h4>Recommendations</h4>
              <p>{feedback.advice}</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

// --- Main Page Component ---
export default function SpeakingResultPage() {
  const location = useLocation();
  const state = location.state || {};
  const navigate = useNavigate();

  const [isLoading, setIsLoading] = useState(true);
  const [progress, setProgress] = useState(0);
  const [feedbackData, setFeedbackData] = useState(null);
  const [playingAudio, setPlayingAudio] = useState(null);
  const audioRefs = useRef({});

  const { examId, userId, exam, mode, isWaiting, testedSpeakingIds } = state;

  // Redirect if missing critical state
  useEffect(() => {
    if (!examId || !userId) navigate("/speaking");
  }, [examId, userId, navigate]);

  // Poll or load feedback
  useEffect(() => {
    let interval, timer, timeout;
    const MAX_POLLS = 20;
    const POLL_INTERVAL = 2500;
    let count = 0;

    const fetchFeedback = async () => {
      count++;
      try {
        const res = await SpeakingApi.getFeedback(examId, userId);
        if (res?.feedbacks?.length > 0) {
          let filtered = res.feedbacks;
          if (
            Array.isArray(testedSpeakingIds) &&
            testedSpeakingIds.length > 0
          ) {
            filtered = res.feedbacks.filter((f) =>
              testedSpeakingIds.includes(f.speakingId)
            );
            if (filtered.length === 0) filtered = res.feedbacks;
          }

          const avg =
            filtered.length > 0
              ? Math.round(
                  (filtered.reduce((s, f) => s + (f.overall || 0), 0) /
                    filtered.length) *
                    10
                ) / 10
              : res.averageOverall || 0;

          setFeedbackData({ feedbacks: filtered, averageOverall: avg });
          setIsLoading(false);
          clearInterval(interval);
          clearInterval(timer);
          clearTimeout(timeout);
        }
      } catch (err) {
        console.error("Error fetching feedback:", err);
      }
    };

    if (isWaiting) {
      setIsLoading(true);
      timer = setInterval(() => setProgress((p) => (p < 90 ? p + 5 : p)), 1000);
      interval = setInterval(fetchFeedback, POLL_INTERVAL);
      fetchFeedback();
      timeout = setTimeout(() => {
        clearInterval(interval);
        clearInterval(timer);
        setIsLoading(false);
      }, MAX_POLLS * POLL_INTERVAL + 1000);
    } else {
      // static result (no polling)
      const fb = state.feedbacks || [];
      const filtered =
        Array.isArray(testedSpeakingIds) && testedSpeakingIds.length > 0
          ? fb.filter((f) => testedSpeakingIds.includes(f.speakingId))
          : fb;
      const avg =
        filtered.length > 0
          ? Math.round(
              (filtered.reduce((s, f) => s + (f.overall || 0), 0) /
                filtered.length) *
                10
            ) / 10
          : state.averageBand || 0;

      setFeedbackData({ feedbacks: filtered, averageOverall: avg });
      setIsLoading(false);
    }

    return () => {
      clearInterval(interval);
      clearInterval(timer);
      clearTimeout(timeout);
    };
  }, [examId, userId, isWaiting, testedSpeakingIds, state]);

  // --- Audio controls ---
  const handlePlay = (id) => {
    const el = audioRefs.current[id];
    if (!el) return;
    if (playingAudio === id) {
      el.pause();
      setPlayingAudio(null);
    } else {
      Object.values(audioRefs.current).forEach((a) => a && a.pause());
      el.play();
      setPlayingAudio(id);
    }
  };

  const handleAudioRef = (id, el) => {
    if (el) {
      audioRefs.current[id] = el;
      el.onended = () => setPlayingAudio(null);
    }
  };

  // --- Loading screen ---
  if (isLoading)
    return (
      <AppLayout title="Speaking Results" sidebar={<GeneralSidebar />}>
        <div className={styles.loadingContainer}>
          <Mic size={48} />
          <h2>AI is analyzing your speaking...</h2>
          <p>Please wait while we evaluate your performance.</p>
          <div className={styles.progressBar}>
            <div
              className={styles.progressFill}
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>
      </AppLayout>
    );

  const { feedbacks = [], averageOverall = 0 } = feedbackData || {};

  return (
    <AppLayout title="Speaking Results" sidebar={<GeneralSidebar />}>
      <div className={styles.pageLayout}>
        <div className={styles.mainContent}>
          {/* Header */}
          <div className={styles.header}>
            <div className={styles.titleSection}>
              <Trophy className={styles.trophyIcon} />
              <div>
                <h1>Speaking Test Results</h1>
                <p>
                  {exam?.examName || "Unknown Exam"} •{" "}
                  {mode === "full" ? "Full Test" : "Single Task"}
                </p>
              </div>
            </div>
            <div className={styles.headerActions}>
              <button className={styles.actionBtn}>
                <Download size={16} /> Download
              </button>
              <button className={styles.actionBtn}>
                <Share2 size={16} /> Share
              </button>
            </div>
          </div>

          {/* Overall score */}
          <div className={styles.overallScore}>
            <div className={styles.overallCard}>
              <h2>Overall Band Score</h2>
              <div className={styles.overallValue}>
                <span className={styles.overallNumber}>{averageOverall}</span>
                <span className={styles.overallMax}>/ 9</span>
              </div>
              <div className={styles.overallBar}>
                <div
                  className={styles.overallFill}
                  style={{ width: `${(averageOverall / 9) * 100}%` }}
                ></div>
              </div>
              <div className={styles.overallLabel}>
                {averageOverall >= 7
                  ? "Excellent!"
                  : averageOverall >= 5
                  ? "Good!"
                  : "Keep Practicing!"}
              </div>
            </div>
          </div>

          {/* Task Breakdown */}
          <div className={styles.tasksSection}>
            <h2>Task Breakdown</h2>
            <div className={styles.tasksGrid}>
              {feedbacks.map((f, i) => {
                const taskId = f.speakingId;
                const aiAnalysis = JSON.parse(f.aiAnalysisJson || "{}");

                return (
                  <div key={taskId || i} className={styles.taskCard}>
                    <div className={styles.taskHeader}>
                      <h3>Task {i + 1}</h3>
                      <div className={styles.taskScore}>
                        <span>{f.overall}</span>
                        <span className={styles.taskScoreMax}>/9</span>
                      </div>
                    </div>

                    {f.audioUrl ? (
                      <>
                        <AudioPlayer
                          audioUrl={f.audioUrl}
                          transcript={f.transcript}
                          onPlay={() => handlePlay(taskId)}
                          onPause={() => handlePlay(taskId)}
                          isPlaying={playingAudio === taskId}
                        />
                        <audio
                          ref={(el) => handleAudioRef(taskId, el)}
                          src={f.audioUrl}
                          preload="metadata"
                          style={{ display: "none" }}
                        />
                      </>
                    ) : (
                      <p>No recording available.</p>
                    )}

                    <div className={styles.scoreBreakdown}>
                      <ScoreCard
                        title="Pronunciation"
                        score={f.pronunciation}
                        color="#ef4444"
                        icon={Volume2}
                      />
                      <ScoreCard
                        title="Fluency"
                        score={f.fluency}
                        color="#3b82f6"
                        icon={TrendingUp}
                      />
                      <ScoreCard
                        title="Coherence"
                        score={f.coherence}
                        color="#10b981"
                        icon={Target}
                      />
                      <ScoreCard
                        title="Lexical Resource"
                        score={f.lexicalResource}
                        color="#f59e0b"
                        icon={Clock}
                      />
                      <ScoreCard
                        title="Grammar"
                        score={f.grammarAccuracy}
                        color="#8b5cf6"
                        icon={CheckCircle}
                      />
                    </div>

                    <FeedbackSection
                      feedback={aiAnalysis.ai_analysis}
                      title="AI Feedback"
                      icon={Mic}
                    />
                  </div>
                );
              })}
            </div>
          </div>

          {/* Retake + Other actions */}
          <div className={styles.actionSection}>
            <button
              className={styles.primaryBtn}
              onClick={() => {
                const firstFeedback = feedbacks?.[0];
                if (mode === "single" && firstFeedback?.speakingId) {
                  // 🔹 Retake đúng task single
                  navigate(
                    `/speaking/test?speakingId=${firstFeedback.speakingId}`,
                    {
                      state: {
                        exam,
                        mode: "single",
                        retakeMode: true,
                      },
                    }
                  );
                } else if (mode === "full") {
                  // 🔹 Retake đúng full test
                  navigate("/speaking/test", {
                    state: {
                      exam,
                      mode: "full",
                      retakeMode: true,
                      tasks: feedbacks.map((f) => ({
                        speakingId: f.speakingId,
                        speakingType: f.speakingType,
                        displayOrder: f.displayOrder,
                        speakingQuestion: f.question || f.speakingQuestion,
                      })),
                    },
                  });
                }
              }}
            >
              <RotateCcw size={20} /> Retake This Test
            </button>

            <button
              className={styles.secondaryBtn}
              onClick={() => navigate("/speaking")}
            >
              Take Another Test
            </button>
          </div>
        </div>
      </div>
    </AppLayout>
  );
}
