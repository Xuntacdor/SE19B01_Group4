import React, { useState, useEffect, useRef } from "react";
import { useLocation, useSearchParams } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as SpeakingApi from "../../Services/SpeakingApi";
import * as UploadApi from "../../Services/UploadApi";
import LoadingComponent from "../../Components/Exam/LoadingComponent";
import MicroCheck from "../../Components/Exam/MicroCheck";
import FloatingDictionaryChat from "../../Components/Dictionary/FloatingDictionaryChat";

import {
  Mic,
  MicOff,
  Play,
  Pause,
  Clock,
  Volume2,
  AlertCircle,
  RotateCcw,
  Trophy,
} from "lucide-react";
import styles from "./SpeakingTestPage.module.css";

export default function SpeakingTest() {
  const location = useLocation();
  const state = location.state || null;

  const [phase, setPhase] = useState("idle");
  const [prepLeft, setPrepLeft] = useState(10);
  const [speakLeft, setSpeakLeft] = useState(60);
  const [isRecording, setIsRecording] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);
  const [recordings, setRecordings] = useState({});
  const [audioUrls, setAudioUrls] = useState({});
  const [uploading, setUploading] = useState({});
  const [speakingTask, setSpeakingTask] = useState(null);
  const [note, setNote] = useState("");
  const [feedback, setFeedback] = useState(null);
  const [showPrepPopup, setShowPrepPopup] = useState(false);

  const mediaRecorderRef = useRef(null);
  const audioRef = useRef(null);
  const prepTimerRef = useRef(null);
  const speakTimerRef = useRef(null);
  const hasSubmittedRef = useRef(false);

  const [params] = useSearchParams();
  const speakingId = params.get("speakingId");

  // ===== Load Task =====
  useEffect(() => {
    const fetchTaskById = async () => {
      if (!speakingId) return;
      try {
        const res = await SpeakingApi.getById(speakingId);
        if (res) setSpeakingTask(res);
      } catch (err) {
        console.error("❌ Failed to load speaking test:", err);
      }
    };
    fetchTaskById();
  }, [speakingId]);
  // ===== Load Existing Feedback (if user already did this task) =====

  const { exam, tasks, task, mode } = state || {};
  const currentExam =
    exam ||
    (speakingTask
      ? { examId: speakingTask.examId, examName: "Single Speaking Task" }
      : undefined);
  const currentMode = mode || (speakingTask ? "single" : undefined);
  const currentTask =
    currentMode === "full" && Array.isArray(tasks)
      ? tasks[0]
      : task || speakingTask;
  const currentId =
    currentMode === "full"
      ? currentTask?.speakingId
      : task?.speakingId || speakingTask?.speakingId;
  useEffect(() => {
    const fetchExistingFeedback = async () => {
      const user = JSON.parse(localStorage.getItem("user"));
      if (!user || !currentId) return;

      try {
        const res = await SpeakingApi.getFeedbackBySpeakingId(
          currentId,
          user.userId
        );
        if (res?.feedback) {
          console.log("📊 Existing feedback found:", res.feedback);
          setFeedback(res.feedback);
          setPhase("result");
        }
      } catch (err) {
        console.warn("ℹ️ No previous feedback found or fetch failed:", err);
      }
    };

    fetchExistingFeedback();
  }, [currentId]);
  // ===== Recording =====
  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const mediaRecorder = new MediaRecorder(stream, {
        mimeType: "audio/webm;codecs=opus",
      });
      const chunks = [];
      let hasStopped = false;

      mediaRecorder.ondataavailable = (e) => chunks.push(e.data);
      mediaRecorder.onstop = async () => {
        if (hasStopped) return;
        hasStopped = true;

        const blob = new Blob(chunks, { type: "audio/webm" });
        setIsRecording(false);
        setUploading((p) => ({ ...p, [currentId]: true }));

        try {
          const res = await UploadApi.uploadAudio(blob);
          const audioUrl =
            res?.url || res?.path || (typeof res === "string" ? res : null);
          if (!audioUrl) throw new Error("No audio URL returned");

          setRecordings((p) => ({ ...p, [currentId]: blob }));
          setAudioUrls((p) => ({ ...p, [currentId]: audioUrl }));
          console.log("✅ Uploaded audio:", audioUrl);
        } catch (err) {
          console.error("❌ Upload failed:", err);
          alert("Upload failed, please try again.");
        } finally {
          setUploading((p) => ({ ...p, [currentId]: false }));
          stream.getTracks().forEach((t) => t.stop());
        }
      };

      mediaRecorder.start();
      mediaRecorderRef.current = mediaRecorder;
      setIsRecording(true);
    } catch (err) {
      alert("Cannot access microphone!");
      console.error("❌ Microphone error:", err);
      setPhase("idle");
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current?.state === "recording") {
      mediaRecorderRef.current.stop();
    }
  };

  // ===== Speak Question =====
  const speakQuestion = (text) => {
    if (!text) return;
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = "en-US";
    utterance.rate = 1;
    utterance.pitch = 1;
    window.speechSynthesis.cancel();
    window.speechSynthesis.speak(utterance);
  };

  // ===== Auto Submit when audio ready =====
  useEffect(() => {
    if (
      audioUrls[currentId] &&
      !hasSubmittedRef.current &&
      !uploading[currentId]
    ) {
      hasSubmittedRef.current = true;
      console.log("🤖 Auto-submit triggered for:", audioUrls[currentId]);
      handleSubmit(true);
    }
  }, [audioUrls, currentId, uploading]);

  // ===== Submit Logic (INLINE FEEDBACK) =====
  const handleSubmit = async () => {
    const user = JSON.parse(localStorage.getItem("user"));
    if (!user || !audioUrls[currentId]) return;

    setPhase("analyzing");
    try {
      const answers = [
        {
          speakingId: currentId,
          displayOrder: 1,
          audioUrl: audioUrls[currentId],
          transcript: "",
          note,
        },
      ];
      const payload = {
        examId: currentExam?.examId,
        userId: user?.userId,
        mode: currentMode,
        answers,
      };

      await SpeakingApi.gradeSpeaking(payload);
      setPhase("waiting");

      // Poll feedback
      for (let i = 0; i < 5; i++) {
        const res = await SpeakingApi.getFeedbackBySpeakingId(
          currentId,
          user.userId
        );
        if (res?.feedback) {
          setFeedback(res.feedback);
          setPhase("result");
          return;
        }
        await new Promise((r) => setTimeout(r, 2000));
      }
      setPhase("waiting");
    } catch (err) {
      console.error("❌ Submit failed:", err);
      setPhase("idle");
    }
  };

  // ===== Countdown =====
  const beginFlow = () => {
    if (phase !== "idle") return;

    // 🔹 Show popup "Get Ready" 3 seconds
    setShowPrepPopup(true);
    setTimeout(() => {
      setShowPrepPopup(false);
      setPhase("recording");
      startRecording();
      setSpeakLeft(60);
      speakTimerRef.current = setInterval(() => {
        setSpeakLeft((s) => {
          if (s <= 1) {
            clearInterval(speakTimerRef.current);
            stopRecording();
            setPhase("done");
            return 0;
          }
          return s - 1;
        });
      }, 1000);
    }, 3000);
  };

  const hasRecording = recordings[currentId];
  const isUploading = uploading[currentId];

  if (!currentTask)
    return (
      <AppLayout title="Speaking Test" sidebar={<GeneralSidebar />}>
        <div className={styles.center}>
          <LoadingComponent text="Loading speaking test..." />
        </div>
      </AppLayout>
    );

  return (
    <AppLayout title="Speaking Test" sidebar={<GeneralSidebar />}>
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.titleSection}>
            <Mic className={styles.titleIcon} />
            <h2>
              {currentTask?.speakingType} — {currentExam?.examName}
            </h2>
          </div>
          <div className={styles.headerRight}>
            <MicroCheck />
          </div>
          <div className={styles.timer}>
            <Clock size={20} /> {speakLeft > 0 ? `${speakLeft}s` : "00:00"}
          </div>
        </div>

        {/* === Popup Get Ready === */}
        {showPrepPopup && (
          <div className={styles.prepPopup}>
            <div className={styles.prepPopupBox}>
              <AlertCircle size={24} />
              <span>Get Ready...</span>
            </div>
          </div>
        )}

        <div className={styles.mainContent}>
          <div className={styles.questionPanel}>
            {/* Always show question */}
            <div className={styles.questionHeader}>
              <h3>Question</h3>
              <button
                className={styles.playBtn}
                onClick={() =>
                  speakQuestion(currentTask?.speakingQuestion || "")
                }
                title="Read aloud"
              >
                <Volume2 size={18} />
              </button>
            </div>

            <div className={styles.questionContent}>
              <p>
                <strong>Q:</strong> {currentTask?.speakingQuestion}
              </p>
            </div>

            {phase === "idle" && (
              <button className={styles.recordBtn} onClick={beginFlow}>
                <Mic size={24} /> Start Recording
              </button>
            )}

            {phase === "recording" && (
              <button className={`${styles.recording} ${styles.recordBtn}`}>
                <MicOff size={24} /> Recording... ({speakLeft}s)
              </button>
            )}

            {phase === "analyzing" && (
              <LoadingComponent text="Analyzing your answer..." />
            )}

            {phase === "waiting" && (
              <LoadingComponent text="Waiting for AI feedback..." />
            )}

            {phase === "result" && feedback && (
              <div className={styles.resultBox}>
                <div className={styles.resultHeader}>
                  <Trophy size={28} color="#2563eb" />
                  <h3>Speaking Result</h3>
                </div>
                {/* === User Answer === */}
                <div className={styles.userAnswerSection}>
                  <h4>Your Answer</h4>
                  {feedback.audioUrl && (
                    <div className={styles.audioPlayer}>
                      <audio controls src={feedback.audioUrl} />
                    </div>
                  )}
                  {feedback.transcript ? (
                    <p className={styles.transcriptText}>
                      {feedback.transcript}
                    </p>
                  ) : (
                    <p className={styles.transcriptTextMuted}>
                      Transcript is not available yet.
                    </p>
                  )}
                </div>

                <div className={styles.scoreContainer}>
                  <div className={`${styles.scoreChip} ${styles.overall}`}>
                    Overall <strong>{feedback.overall}</strong>
                  </div>
                  <div className={`${styles.scoreChip} ${styles.pron}`}>
                    Pronunciation <strong>{feedback.pronunciation}</strong>
                  </div>
                  <div className={`${styles.scoreChip} ${styles.grammar}`}>
                    Grammar <strong>{feedback.grammarAccuracy}</strong>
                  </div>
                  <div className={`${styles.scoreChip} ${styles.fluency}`}>
                    Fluency <strong>{feedback.fluency}</strong>
                  </div>
                  <div className={`${styles.scoreChip} ${styles.coherence}`}>
                    Coherence <strong>{feedback.coherence}</strong>
                  </div>
                </div>

                {feedback?.aiAnalysisJson && (
                  <div className={styles.feedbackText}>
                    <h4>AI Feedback</h4>
                    <p>
                      {JSON.parse(feedback.aiAnalysisJson)?.ai_analysis
                        ?.overview || "No feedback text."}
                    </p>
                  </div>
                )}

                <button
                  className={styles.recordAgainBtn}
                  onClick={() => {
                    setPhase("idle");
                    hasSubmittedRef.current = false;
                    setFeedback(null);
                  }}
                >
                  <RotateCcw size={20} /> Retake
                </button>
              </div>
            )}

            {hasRecording && phase === "done" && (
              <>
                <div className={styles.audioPlayer}>
                  <audio
                    ref={audioRef}
                    src={audioUrls[currentId]}
                    onEnded={() => setIsPlaying(false)}
                    controls
                  />
                </div>
                <div className={styles.audioInfo}>
                  <Volume2 size={16} /> <span>Your recording is ready</span>
                </div>
              </>
            )}
          </div>

          <aside className={styles.notePanel}>
            <h4>My Notes</h4>
            <textarea
              placeholder="Write your notes here..."
              value={note}
              onChange={(e) => setNote(e.target.value)}
            />
          </aside>
        </div>
      </div>
      <FloatingDictionaryChat />
    </AppLayout>
  );
}
