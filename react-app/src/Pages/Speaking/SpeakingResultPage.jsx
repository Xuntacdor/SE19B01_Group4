// react-app/src/Pages/Speaking/SpeakingResultPage.jsx
import React, { useState, useEffect, useRef } from "react";

import { useLocation, useNavigate } from "react-router-dom";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import AppLayout from "../../Components/Layout/AppLayout"; // Assuming AppLayout is used
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

// --- AudioPlayer Component (Keep as is) ---
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
      {/* Conditionally render transcript only if it's not empty */}
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

// --- ScoreCard Component (Keep as is) ---
function ScoreCard({ title, score, maxScore = 9, color, icon: Icon }) {
  const percentage = (score / maxScore) * 100;
  const scoreText =
    score >= 7 ? "Excellent" : score >= 5 ? "Good" : "Needs Improvement";

  return (
    <div className={styles.scoreCard}>
      <div className={styles.scoreHeader}>
        <Icon size={20} className={styles.scoreIcon} style={{ color }} />
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

// --- FeedbackSection Component (Keep as is) ---
function FeedbackSection({ feedback, title, icon: Icon }) {
  const [isExpanded, setIsExpanded] = useState(true); // Default to expanded

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

          {feedback.strengths && feedback.strengths.length > 0 && (
            <div className={styles.strengths}>
              <h4>Strengths</h4>
              <ul>
                {feedback.strengths.map((strength, index) => (
                  <li key={index}>
                    <CheckCircle size={14} className={styles.checkIcon} />
                    {strength}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {feedback.weaknesses && feedback.weaknesses.length > 0 && (
            <div className={styles.weaknesses}>
              <h4>Areas for Improvement</h4>
              <ul>
                {feedback.weaknesses.map((weakness, index) => (
                  <li key={index}>
                    <AlertTriangle size={14} className={styles.warningIcon} />
                    {weakness}
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
  const { state } = useLocation();
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(true);
  const [progress, setProgress] = useState(0);
  const [feedbackData, setFeedbackData] = useState(null);
  const [playingAudio, setPlayingAudio] = useState(null); // Tracks ID of playing audio
  const audioElementsRef = useRef({}); // Use ref to store audio elements

  // Check initial state
  if (!state || !state.examId || !state.userId) {
    // Redirect if essential info is missing
    useEffect(() => {
      navigate("/speaking"); // Redirect to speaking page if state is invalid
    }, [navigate]);

    return (
      <AppLayout title="Speaking Results" sidebar={<GeneralSidebar />}>
        <div className={styles.center}>
          <h2>Loading result data...</h2>
          <p>If you see this for a long time, please go back.</p>
          <button
            onClick={() => navigate("/speaking")}
            className={styles.backBtn}
          >
            ← Back to Speaking
          </button>
        </div>
      </AppLayout>
    );
  }

  const { examId, userId, exam, mode, isWaiting } = state;

  // Fetch feedback data via polling if `isWaiting`
  useEffect(() => {
    let interval;
    let progressTimer;
    let timeoutId;
    let pollCount = 0;
    const MAX_POLLS = 20; // Maximum 20 polls (50 seconds)
    const POLL_INTERVAL = 2500; // 2.5 seconds

    const fetchFeedback = async () => {
      try {
        pollCount++;
        console.log(
          `[SpeakingResult] Fetching feedback (attempt ${pollCount}/${MAX_POLLS}) for examId: ${examId}, userId: ${userId}`
        );
        const res = await SpeakingApi.getFeedback(examId, userId);
        console.log("[SpeakingResult] Feedback API response:", res);

        if (res?.feedbacks?.length > 0) {
          console.log("[SpeakingResult] Feedback found, stopping polling");
          setFeedbackData(res);
          setIsLoading(false);
          clearInterval(interval);
          clearInterval(progressTimer);
          clearTimeout(timeoutId);
        } else {
          console.log(
            "[SpeakingResult] No feedback yet, continuing to poll..."
          );
        }
      } catch (error) {
        console.error("[SpeakingResult] Failed to fetch feedback:", error);
        // Only stop polling on non-404 errors or after max polls
        if (error.response?.status !== 404 && pollCount >= MAX_POLLS) {
          console.error(
            "[SpeakingResult] Max polls reached or non-404 error, stopping."
          );
          setIsLoading(false);
          setFeedbackData({
            feedbacks: [],
            averageOverall: 0,
            error:
              "Could not retrieve feedback. Please try the test again later.",
          });
          clearInterval(interval);
          clearInterval(progressTimer);
          clearTimeout(timeoutId);
        } else if (error.response?.status === 404) {
          console.log(
            "[SpeakingResult] Feedback not ready (404), continuing..."
          );
        } else {
          console.log("[SpeakingResult] Other error, continuing poll...");
        }
      }
    };

    if (isWaiting) {
      setIsLoading(true); // Ensure loading state is true initially
      setProgress(0);
      progressTimer = setInterval(() => {
        setProgress((p) => (p < 90 ? p + 5 : p));
      }, 1000);

      interval = setInterval(fetchFeedback, POLL_INTERVAL);
      fetchFeedback(); // Initial fetch

      // Set timeout to stop polling
      timeoutId = setTimeout(() => {
        console.log("[SpeakingResult] Polling timeout reached, stopping...");
        if (isLoading) {
          // Check if still loading
          console.error("[SpeakingResult] Timeout reached, showing error");
          setIsLoading(false);
          setFeedbackData({
            feedbacks: [],
            averageOverall: 0,
            error: "Feedback generation timed out. Please try again later.",
          });
        }
        clearInterval(interval);
        clearInterval(progressTimer);
      }, MAX_POLLS * POLL_INTERVAL + 1000); // Add a small buffer
    } else {
      // If not waiting, assume feedback is already in state (though less likely now)
      setIsLoading(false);
      setFeedbackData({
        feedbacks: state.feedbacks || [],
        averageOverall: state.averageBand || 0,
      });
    }

    // Cleanup function
    return () => {
      clearInterval(interval);
      clearInterval(progressTimer);
      clearTimeout(timeoutId);
      // Stop any playing audio when component unmounts
      Object.values(audioElementsRef.current).forEach((audio) => {
        if (audio) audio.pause();
      });
    };
  }, [examId, userId, isWaiting, isLoading, navigate]); // Added isLoading and navigate

  // Audio playback controls
  const handlePlayAudio = (taskId) => {
    const currentAudio = audioElementsRef.current[taskId];
    if (!currentAudio) return;

    if (playingAudio === taskId) {
      // Pause current audio
      currentAudio.pause();
      setPlayingAudio(null);
    } else {
      // Stop all other audio
      Object.entries(audioElementsRef.current).forEach(([id, audio]) => {
        if (audio && id !== String(taskId)) {
          // Compare as strings if needed
          audio.pause();
        }
      });

      // Play new audio
      currentAudio.play();
      setPlayingAudio(taskId);
    }
  };

  // Store audio element refs
  const handleAudioRef = (taskId, audioElement) => {
    if (audioElement) {
      audioElementsRef.current[taskId] = audioElement;
      // Clean up previous listeners if element re-renders
      audioElement.removeEventListener("ended", () => setPlayingAudio(null));
      audioElement.removeEventListener("play", () => setPlayingAudio(taskId));
      audioElement.removeEventListener("pause", () => {
        if (playingAudio === taskId) setPlayingAudio(null);
      }); // Only reset if it was the one playing

      // Add new listeners
      audioElement.addEventListener("ended", () => setPlayingAudio(null));
      audioElement.addEventListener("play", () => setPlayingAudio(taskId));
      audioElement.addEventListener("pause", () => {
        if (playingAudio === taskId) setPlayingAudio(null);
      });
    } else {
      // Clean up ref if element is removed
      delete audioElementsRef.current[taskId];
    }
  };

  // Loading state render
  if (isLoading) {
    return (
      <AppLayout title="Speaking Results" sidebar={<GeneralSidebar />}>
        <div className={styles.loadingContainer}>
          <div className={styles.loadingContent}>
            <div className={styles.loadingIcon}>
              <Mic size={48} />
            </div>
            <h2>AI is analyzing your speaking...</h2>
            <p>
              Please wait while we evaluate your pronunciation, fluency, and
              coherence.
            </p>
            <div className={styles.progressBar}>
              <div
                className={styles.progressFill}
                style={{ width: `${progress}%` }}
              ></div>
            </div>
            <div className={styles.loadingSteps}>
              <div className={styles.step}>
                <CheckCircle size={16} />
                <span>Audio processed</span>
              </div>
              <div className={styles.step}>
                <CheckCircle size={16} />
                <span>Transcription complete</span>
              </div>
              <div className={styles.step}>
                <div className={styles.spinner}></div>
                <span>AI evaluation in progress...</span>
              </div>
            </div>
          </div>
        </div>
      </AppLayout>
    );
  }

  // Extract data after loading
  const { feedbacks, averageOverall, error } = feedbackData || {
    feedbacks: [],
    averageOverall: 0,
    error: null,
  };

  // Error state render
  if (error) {
    return (
      <AppLayout title="Speaking Results" sidebar={<GeneralSidebar />}>
        <div className={styles.pageLayout}>
          <div className={styles.mainContent}>
            <div className={styles.errorContainer}>
              <h2>Feedback Generation Error</h2>
              <p>{error}</p>
              <button
                onClick={() => navigate("/speaking")} // Go back to speaking page on error
                className={styles.retryBtn}
              >
                Go Back
              </button>
            </div>
          </div>
        </div>
      </AppLayout>
    );
  }

  // --- Main Render ---
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
                <Download size={16} /> Download Report
              </button>
              <button className={styles.actionBtn}>
                <Share2 size={16} /> Share
              </button>
            </div>
          </div>

          {/* Overall Score */}
          <div className={styles.overallScore}>
            <div className={styles.overallCard}>
              <div className={styles.overallHeader}>
                <h2>Overall Band Score</h2>
                <div className={styles.overallValue}>
                  <span className={styles.overallNumber}>{averageOverall}</span>
                  <span className={styles.overallMax}>/ 9</span>
                </div>
              </div>
              <div className={styles.overallBar}>
                <div
                  className={styles.overallFill}
                  style={{ width: `${(averageOverall / 9) * 100}%` }}
                ></div>
              </div>
              <div className={styles.overallLabel}>
                {averageOverall >= 7
                  ? "Excellent Performance!"
                  : averageOverall >= 5
                  ? "Good Performance!"
                  : "Keep Practicing!"}
              </div>
            </div>
          </div>

          {/* Individual Task Results */}
          <div className={styles.tasksSection}>
            <h2>Task Breakdown</h2>
            <div className={styles.tasksGrid}>
              {feedbacks.map((feedback, index) => {
                const taskId = feedback.speakingId; // Use speakingId from feedback
                const recordingUrl = feedback.audioUrl;
                const transcriptText = feedback.transcript;
                const aiAnalysis = JSON.parse(feedback.aiAnalysisJson || "{}");

                return (
                  <div
                    key={feedback.speakingAttemptId || index}
                    className={styles.taskCard}
                  >
                    <div className={styles.taskHeader}>
                      <h3>Task {index + 1}</h3>
                      <div className={styles.taskScore}>
                        <span className={styles.taskScoreValue}>
                          {feedback.overall}
                        </span>
                        <span className={styles.taskScoreMax}>/ 9</span>
                      </div>
                    </div>

                    {/* Audio Player - Uses fetched data */}
                    {recordingUrl && (
                      <>
                        <AudioPlayer
                          audioUrl={recordingUrl}
                          transcript={transcriptText}
                          onPlay={() => handlePlayAudio(taskId)}
                          onPause={() => handlePlayAudio(taskId)}
                          isPlaying={playingAudio === taskId}
                        />
                        {/* Preload audio */}
                        <audio
                          ref={(el) => handleAudioRef(taskId, el)}
                          src={recordingUrl}
                          preload="metadata"
                          style={{ display: "none" }} // Hide the default player
                        />
                      </>
                    )}
                    {!recordingUrl && (
                      <p>No recording available for this task.</p>
                    )}

                    {/* Score Breakdown */}
                    <div className={styles.scoreBreakdown}>
                      <ScoreCard
                        title="Pronunciation"
                        score={feedback.pronunciation}
                        color="#ef4444"
                        icon={Volume2}
                      />
                      <ScoreCard
                        title="Fluency"
                        score={feedback.fluency}
                        color="#3b82f6"
                        icon={TrendingUp}
                      />
                      <ScoreCard
                        title="Coherence"
                        score={feedback.coherence}
                        color="#10b981"
                        icon={Target}
                      />
                      <ScoreCard
                        title="Lexical Resource"
                        score={feedback.lexicalResource}
                        color="#f59e0b"
                        icon={Clock}
                      />
                      <ScoreCard
                        title="Grammar"
                        score={feedback.grammarAccuracy}
                        color="#8b5cf6"
                        icon={CheckCircle}
                      />
                    </div>

                    {/* AI Feedback */}
                    <div className={styles.feedbackContainer}>
                      <FeedbackSection
                        feedback={aiAnalysis.ai_analysis} // Access nested property
                        title="AI Analysis"
                        icon={Mic}
                      />
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Action Buttons */}
          <div className={styles.actionSection}>
            <button
              className={styles.primaryBtn}
              onClick={() => navigate("/speaking")}
            >
              <RotateCcw size={20} /> Take Another Test
            </button>
            <button
              className={styles.secondaryBtn}
              onClick={() => navigate("/home")}
            >
              Back to Home
            </button>
          </div>
        </div>
      </div>
    </AppLayout>
  );
}
