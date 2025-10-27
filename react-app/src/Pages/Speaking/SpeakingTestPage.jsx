import React, { useState, useEffect, useRef } from "react";
import { useLocation, useNavigate, useSearchParams } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as SpeakingApi from "../../Services/SpeakingApi";
import * as UploadApi from "../../Services/UploadApi";
import LoadingComponent from "../../Components/Exam/LoadingComponent";
import useExamTimer from "../../Hook/useExamTimer";
import MicroCheck from "../../Components/Exam/MicroCheck";
import {
  Mic,
  MicOff,
  Play,
  Pause,
  Clock,
  Volume2,
  CheckCircle,
  AlertCircle,
  RotateCcw,
} from "lucide-react";
import styles from "./SpeakingTestPage.module.css";

export default function SpeakingTest() {
  const location = useLocation();
  const state = location.state || null;
  const navigate = useNavigate();
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isRecording, setIsRecording] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);
  const [recordings, setRecordings] = useState({});
  const [audioUrls, setAudioUrls] = useState({});
  const [submitting, setSubmitting] = useState(false);
  const [recordingTime, setRecordingTime] = useState(0);
  const [uploading, setUploading] = useState({});
  const [speakingTask, setSpeakingTask] = useState(null);

  const mediaRecorderRef = useRef(null);
  const audioRef = useRef(null);
  const recordingIntervalRef = useRef(null);

  const [params] = useSearchParams();
  const speakingId = params.get("speakingId");

  // --- FETCH khi retake / single ---
  useEffect(() => {
    const fetchTaskById = async () => {
      if (!speakingId) return;
      try {
        const res = await SpeakingApi.getById(speakingId);
        if (res) {
          setSpeakingTask(res);
        }
      } catch (err) {
        console.error("Failed to load speaking test:", err);
      }
    };
    fetchTaskById();
  }, [speakingId]);

  // --- Resolve part index (Part 1/2/3 buttons) ---
  function resolvePartIndex(tasks, partNumber) {
    if (!Array.isArray(tasks) || tasks.length === 0) return -1;
    let idx = tasks.findIndex((t) => Number(t?.displayOrder) === partNumber);
    if (idx !== -1) return idx;
    idx = tasks.findIndex((t) =>
      (t?.speakingType || "").toLowerCase().includes(`part${partNumber}`)
    );
    return idx !== -1 ? idx : partNumber - 1;
  }

  // --- Destructure safely ---
  const { exam, tasks, task, mode, duration } = state || {};
  const currentExam =
    exam ||
    (speakingTask
      ? { examId: speakingTask.examId, examName: "Single Speaking Task" }
      : undefined);
  const currentMode = mode || (speakingTask ? "single" : undefined);
  const currentTask =
    currentMode === "full" && Array.isArray(tasks)
      ? tasks[currentIndex]
      : task || speakingTask;
  const currentId =
    currentMode === "full"
      ? currentTask?.speakingId
      : task?.speakingId || speakingTask?.speakingId;

  // --- Timer ---
  const { timeLeft, formatTime } = useExamTimer(
    duration || exam?.duration || currentTask?.duration || 15,
    submitting
  );

  // --- Auto redirect if already done ---
  useEffect(() => {
    if (state?.retakeMode) return; // bỏ qua khi đang làm lại

    const checkIfDone = async () => {
      try {
        const user = JSON.parse(localStorage.getItem("user"));
        if (!user) return;

        if (mode === "single") {
          // 🔹 chỉ kiểm tra đúng speaking task này
          const res = await SpeakingApi.getFeedbackBySpeakingId(
            task?.speakingId || speakingId,
            user.userId
          );
          if (res?.feedback) {
            navigate("/speaking/result", {
              state: {
                examId: res.examId,
                userId: user.userId,
                exam,
                mode: "single",
                isWaiting: false,
                feedbacks: [res.feedback],
                averageBand: res.feedback.overall,
              },
            });
          }
        } else if (mode === "full" && exam?.examId) {
          // 🔹 kiểm tra full exam
          const res = await SpeakingApi.getFeedback(exam.examId, user.userId);
          if (res?.feedbacks?.length > 0) {
            navigate("/speaking/result", {
              state: {
                examId: exam.examId,
                userId: user.userId,
                exam,
                mode: "full",
                isWaiting: false,
                feedbacks: res.feedbacks,
                averageBand: res.averageOverall,
              },
            });
          }
        }
      } catch (err) {
        console.warn("No previous result found for this exam");
      }
    };

    checkIfDone();
  }, [exam, navigate, mode, state?.retakeMode]);

  // --- Text to Speech ---
  const speakQuestion = (text) => {
    if (!window.speechSynthesis) {
      alert("Speech synthesis not supported on this browser.");
      return;
    }
    const utter = new SpeechSynthesisUtterance(text);
    utter.lang = "en-US";
    utter.rate = 1;
    utter.pitch = 1;
    window.speechSynthesis.cancel();
    window.speechSynthesis.speak(utter);
  };

  // --- Recording ---
  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const options = { mimeType: "audio/webm;codecs=opus" };
      const mediaRecorder = new MediaRecorder(stream, options);
      const chunks = [];

      mediaRecorder.ondataavailable = (e) => chunks.push(e.data);
      mediaRecorder.onstop = async () => {
        const blob = new Blob(chunks, { type: "audio/webm" });
        const url = URL.createObjectURL(blob);
        setRecordings((p) => ({ ...p, [currentId]: blob }));
        setAudioUrls((p) => ({ ...p, [currentId]: url }));
        await uploadAudio(blob, currentId);
        stream.getTracks().forEach((t) => t.stop());
      };

      mediaRecorder.start();
      mediaRecorderRef.current = mediaRecorder;
      setIsRecording(true);
      setRecordingTime(0);
      recordingIntervalRef.current = setInterval(
        () => setRecordingTime((t) => t + 1),
        1000
      );
    } catch (err) {
      alert("Cannot access microphone!");
      console.error(err);
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      clearInterval(recordingIntervalRef.current);
      setIsRecording(false);
    }
  };

  const playRecording = () => {
    if (audioRef.current) {
      if (isPlaying) {
        audioRef.current.pause();
        setIsPlaying(false);
      } else {
        audioRef.current.play();
        setIsPlaying(true);
      }
    }
  };

  const uploadAudio = async (blob, taskId) => {
    setUploading((p) => ({ ...p, [taskId]: true }));
    try {
      const res = await UploadApi.uploadAudio(blob);
      setAudioUrls((p) => ({ ...p, [taskId]: res.url }));
    } catch (err) {
      alert("Upload failed");
    } finally {
      setUploading((p) => ({ ...p, [taskId]: false }));
    }
  };

  // --- Submit ---
  const handleSubmit = async () => {
    if (!recordings || Object.keys(recordings).length === 0) {
      alert("No recordings found!");
      return;
    }
    setSubmitting(true);
    try {
      const user = JSON.parse(localStorage.getItem("user"));
      const speakingTasks =
        currentMode === "full" && Array.isArray(tasks) ? tasks : [currentTask];
      const answers = speakingTasks.map((t) => ({
        speakingId: t.speakingId,
        displayOrder: t.displayOrder || 1,
        audioUrl: audioUrls[t.speakingId],
        transcript: "",
      }));
      const payload = {
        examId: currentExam?.examId,
        userId: user?.userId,
        mode: currentMode,
        answers,
      };
      await SpeakingApi.gradeSpeaking(payload);
      navigate("/speaking/result", {
        state: {
          examId: currentExam?.examId,
          userId: user?.userId,
          exam: currentExam,
          mode: currentMode,
          recordings: audioUrls,
          isWaiting: true,
          testedSpeakingIds: answers.map((a) => a.speakingId),
        },
      });
    } catch (err) {
      alert("Submit failed");
    } finally {
      setSubmitting(false);
    }
  };

  const hasRecording = recordings[currentId];
  const isUploading = uploading[currentId];
  const formatRecordingTime = (s) =>
    `${Math.floor(s / 60)}:${(s % 60).toString().padStart(2, "0")}`;

  if (!currentTask) {
    return (
      <AppLayout title="Speaking Test" sidebar={<GeneralSidebar />}>
        <div className={styles.center}>
          <LoadingComponent text="Loading speaking test..." />
        </div>
      </AppLayout>
    );
  }

  return (
    <AppLayout title="Speaking Test" sidebar={<GeneralSidebar />}>
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.titleSection}>
            <Mic className={styles.titleIcon} />
            <h2>
              {currentMode === "full"
                ? `Full Speaking Test — ${currentExam?.examName}`
                : `${currentTask?.speakingType} — ${currentExam?.examName}`}
            </h2>
          </div>
          <div className={styles.headerRight}>
            <MicroCheck />
          </div>
          <div className={styles.timer}>
            <Clock size={20} /> {formatTime(timeLeft)}
          </div>
        </div>

        <div className={styles.mainContent}>
          <div className={styles.questionPanel}>
            <div className={styles.questionHeader}>
              <h3>Question {currentMode === "full" ? currentIndex + 1 : 1}</h3>
              <button
                className={styles.playBtn}
                onClick={() => speakQuestion(currentTask?.speakingQuestion)}
              >
                <Volume2 size={18} />
              </button>
            </div>
            <div className={styles.questionContent}>
              <p>{currentTask?.speakingQuestion}</p>
            </div>
            <div className={styles.instructions}>
              <AlertCircle size={16} />
              <span>Speak clearly and naturally.</span>
            </div>
          </div>

          <div className={styles.recordingPanel}>
            <div className={styles.recordingControls}>
              {!hasRecording ? (
                <button
                  className={`${styles.recordBtn} ${
                    isRecording ? styles.recording : ""
                  }`}
                  onClick={isRecording ? stopRecording : startRecording}
                  disabled={isUploading}
                >
                  {isRecording ? <MicOff size={24} /> : <Mic size={24} />}
                  {isRecording ? "Stop Recording" : "Start Recording"}
                </button>
              ) : (
                <div className={styles.recordingActions}>
                  <button className={styles.playBtn} onClick={playRecording}>
                    {isPlaying ? <Pause size={20} /> : <Play size={20} />}
                    {isPlaying ? "Pause" : "Play"}
                  </button>
                  <button
                    className={styles.recordAgainBtn}
                    onClick={() => {
                      setRecordings((p) => {
                        const n = { ...p };
                        delete n[currentId];
                        return n;
                      });
                      setAudioUrls((p) => {
                        const n = { ...p };
                        delete n[currentId];
                        return n;
                      });
                    }}
                  >
                    <RotateCcw size={20} /> Record Again
                  </button>
                </div>
              )}
              {isRecording && (
                <div className={styles.recordingTimer}>
                  <div className={styles.recordingIndicator}></div>
                  <span>Recording: {formatRecordingTime(recordingTime)}</span>
                </div>
              )}
              {isUploading && (
                <div className={styles.uploadingIndicator}>
                  <div className={styles.spinner}></div>
                  <span>Uploading...</span>
                </div>
              )}
            </div>

            {hasRecording && (
              <div className={styles.audioPlayer}>
                <audio
                  ref={audioRef}
                  src={audioUrls[currentId]}
                  onEnded={() => setIsPlaying(false)}
                />
                <div className={styles.audioInfo}>
                  <Volume2 size={16} /> <span>Your recording is ready</span>
                </div>
              </div>
            )}
          </div>

          <div className={styles.navigation}>
            {currentMode === "full" && Array.isArray(tasks) && (
              <div className={styles.taskNavigation}>
                {[1, 2, 3].map((n) => {
                  const idx = resolvePartIndex(tasks, n);
                  return (
                    <button
                      key={n}
                      onClick={() => setCurrentIndex(idx)}
                      className={`${styles.navBtn} ${
                        currentIndex === idx ? styles.activeNavBtn : ""
                      }`}
                    >
                      Part {n}
                    </button>
                  );
                })}
              </div>
            )}

            <div className={styles.submitSection}>
              <button
                className={styles.submitBtn}
                onClick={handleSubmit}
                disabled={submitting || !hasRecording}
              >
                {submitting ? (
                  <>
                    <div className={styles.spinner}></div> Submitting...
                  </>
                ) : (
                  <>
                    <CheckCircle size={20} /> Submit Test
                  </>
                )}
              </button>
              <button className={styles.backBtn} onClick={() => navigate(-1)}>
                ← Back
              </button>
            </div>
          </div>
        </div>
      </div>
      {submitting && <LoadingComponent text="Submitting your test..." />}
    </AppLayout>
  );
}
