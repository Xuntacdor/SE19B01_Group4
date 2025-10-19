import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
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
      {transcript && (
        <div className={styles.transcriptSection}>
          <h4>Transcript:</h4>
          <p>{transcript}</p>
        </div>
      )}
    </div>
  );
}

function ScoreCard({ title, score, maxScore = 9, color, icon: Icon }) {
  const percentage = (score / maxScore) * 100;

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
      <div className={styles.scoreLabel}>
        {score >= 7 ? "Excellent" : score >= 5 ? "Good" : "Needs Improvement"}
      </div>
    </div>
  );
}

function FeedbackSection({ feedback, title, icon: Icon }) {
  const [isExpanded, setIsExpanded] = useState(false);

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

export default function SpeakingResultPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(true);
  const [progress, setProgress] = useState(0);
  const [feedbackData, setFeedbackData] = useState(null);
  const [playingAudio, setPlayingAudio] = useState(null);
  const [audioElements, setAudioElements] = useState({});

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

  const { examId, userId, exam, mode, recordings, transcripts, isWaiting } =
    state;

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
        console.log(`[SpeakingResult] Fetching feedback (attempt ${pollCount}/${MAX_POLLS}) for examId: ${examId}, userId: ${userId}`);
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
          console.log("[SpeakingResult] No feedback yet, continuing to poll...");
        }
      } catch (error) {
        console.error("[SpeakingResult] Failed to fetch feedback:", error);
        console.error("[SpeakingResult] Error details:", error.response?.data || error.message);
        
        // If it's a 404, it means feedback is not ready yet, continue polling
        if (error.response?.status === 404) {
          console.log("[SpeakingResult] Feedback not ready yet (404), continuing to poll...");
        } else {
          // For other errors, show error after some time
          console.error("[SpeakingResult] Non-404 error, will retry...");
        }
      }
    };

    if (isWaiting) {
      setProgress(0);
      progressTimer = setInterval(() => {
        setProgress((p) => (p < 90 ? p + 5 : p));
      }, 1000);
      
      interval = setInterval(fetchFeedback, POLL_INTERVAL);
      fetchFeedback();
      
      // Set timeout to stop polling after maximum attempts
      timeoutId = setTimeout(() => {
        console.log("[SpeakingResult] Polling timeout reached, stopping...");
        clearInterval(interval);
        clearInterval(progressTimer);
        
        // Show error message if no feedback found
        if (pollCount >= MAX_POLLS) {
          console.error("[SpeakingResult] Maximum polling attempts reached, showing error");
          setIsLoading(false);
          setFeedbackData({
            feedbacks: [],
            averageOverall: 0,
            error: "Feedback generation is taking longer than expected. Please try again later."
          });
        }
      }, MAX_POLLS * POLL_INTERVAL);
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
      clearTimeout(timeoutId);
    };
  }, [examId, userId, isWaiting, state]);

  const handlePlayAudio = (taskId) => {
    if (playingAudio === taskId) {
      // Pause current audio
      if (audioElements[taskId]) {
        audioElements[taskId].pause();
      }
      setPlayingAudio(null);
    } else {
      // Stop all other audio
      Object.values(audioElements).forEach((audio) => audio.pause());

      // Play new audio
      if (audioElements[taskId]) {
        audioElements[taskId].play();
        setPlayingAudio(taskId);
      }
    }
  };

  const handleAudioRef = (taskId, audioElement) => {
    if (audioElement) {
      setAudioElements((prev) => ({
        ...prev,
        [taskId]: audioElement,
      }));

      audioElement.addEventListener("ended", () => setPlayingAudio(null));
      audioElement.addEventListener("play", () => setPlayingAudio(taskId));
      audioElement.addEventListener("pause", () => setPlayingAudio(null));
    }
  };

  if (isLoading) {
    return (
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
    );
  }

  const { feedbacks, averageOverall, error } = feedbackData || {
    feedbacks: [],
    averageOverall: 0,
    error: null,
  };

  // Show error message if there's an error
  if (error) {
    return (
      <div className={styles.pageLayout}>
        <GeneralSidebar />
        <div className={styles.mainContent}>
          <div className={styles.errorContainer}>
            <h2>Feedback Generation Error</h2>
            <p>{error}</p>
            <button 
              onClick={() => window.location.reload()} 
              className={styles.retryBtn}
            >
              Try Again
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
              <Download size={16} />
              Download Report
            </button>
            <button className={styles.actionBtn}>
              <Share2 size={16} />
              Share
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
              const taskId = feedback.speakingId;
              const recording = recordings?.[taskId];
              const transcript = transcripts?.[taskId];
              const aiAnalysis = JSON.parse(feedback.aiAnalysisJson || "{}");

              return (
                <div key={index} className={styles.taskCard}>
                  <div className={styles.taskHeader}>
                    <h3>Task {index + 1}</h3>
                    <div className={styles.taskScore}>
                      <span className={styles.taskScoreValue}>
                        {feedback.overall}
                      </span>
                      <span className={styles.taskScoreMax}>/ 9</span>
                    </div>
                  </div>

                  {/* Audio Player */}
                  {recording && (
                    <>
                      <AudioPlayer
                        audioUrl={recording}
                        transcript={transcript}
                        onPlay={() => handlePlayAudio(taskId)}
                        onPause={() => handlePlayAudio(taskId)}
                        isPlaying={playingAudio === taskId}
                      />
                      <audio
                        ref={(el) => handleAudioRef(taskId, el)}
                        src={recording}
                        preload="metadata"
                      />
                    </>
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
                      feedback={aiAnalysis.ai_analysis}
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
            <RotateCcw size={20} />
            Take Another Test
          </button>
          <button className={styles.secondaryBtn} onClick={() => navigate("/")}>
            Back to Home
          </button>
        </div>
      </div>
    </div>
  );
}
