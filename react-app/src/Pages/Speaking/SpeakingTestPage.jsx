import React, { useState, useEffect, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as SpeakingApi from "../../Services/SpeakingApi";
import * as UploadApi from "../../Services/UploadApi";
import LoadingComponent from "../../Components/Exam/LoadingComponent";
import useExamTimer from "../../Hook/useExamTimer";
import {
  Mic,
  MicOff,
  Play,
  Pause,
  Square,
  Upload,
  Clock,
  Volume2,
  CheckCircle,
  AlertCircle,
  RotateCcw,
} from "lucide-react";
import styles from "./SpeakingTestPage.module.css";

export default function SpeakingTest() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isRecording, setIsRecording] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);
  const [recordings, setRecordings] = useState({});
  const [audioUrls, setAudioUrls] = useState({});
  const [submitting, setSubmitting] = useState(false);
  const [recordingTime, setRecordingTime] = useState(0);
  const [uploading, setUploading] = useState({});

  const mediaRecorderRef = useRef(null);
  const audioRef = useRef(null);
  const recordingIntervalRef = useRef(null);

  if (!state) {
    return (
      <AppLayout title="Speaking Test" sidebar={<GeneralSidebar />}>
        <div className={styles.center}>
          <h2>No exam selected</h2>
          <button onClick={() => navigate(-1)} className={styles.backBtn}>
            ← Back
          </button>
        </div>
      </AppLayout>
    );
  }

  const { exam, tasks, task, mode, duration } = state;
  const currentTask =
    mode === "full" && Array.isArray(tasks) ? tasks[currentIndex] : task;
  const currentId =
    mode === "full" ? currentTask?.speakingId : task?.speakingId;

  // Timer logic
  const { timeLeft, formatTime } = useExamTimer(duration || 15, submitting);

  // Recording functions
  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

      // Sử dụng MIME type được Whisper hỗ trợ, với fallback
      let options = {};
      if (MediaRecorder.isTypeSupported("audio/webm;codecs=opus")) {
        options = { mimeType: "audio/webm;codecs=opus" };
      } else if (MediaRecorder.isTypeSupported("audio/webm")) {
        options = { mimeType: "audio/webm" };
      } else if (MediaRecorder.isTypeSupported("audio/mp4")) {
        options = { mimeType: "audio/mp4" };
      } else if (MediaRecorder.isTypeSupported("audio/wav")) {
        options = { mimeType: "audio/wav" };
      }

      console.log("Using MediaRecorder options:", options);
      const mediaRecorder = new MediaRecorder(stream, options);
      const audioChunks = [];

      mediaRecorder.ondataavailable = (event) => {
        audioChunks.push(event.data);
      };

      mediaRecorder.onstop = async () => {
        // Tạo blob với format phù hợp
        const mimeType = options.mimeType || "audio/webm";
        const audioBlob = new Blob(audioChunks, { type: mimeType });
        const audioUrl = URL.createObjectURL(audioBlob);

        console.log("Created audio blob:", {
          type: mimeType,
          size: audioBlob.size,
          chunks: audioChunks.length,
        });

        setRecordings((prev) => ({
          ...prev,
          [currentId]: audioBlob,
        }));
        setAudioUrls((prev) => ({
          ...prev,
          [currentId]: audioUrl,
        }));

        // Auto-upload after recording (no transcription here)
        console.log(`Auto-uploading audio for task ${currentId}`);
        await uploadAudio(audioBlob, currentId);

        // Stop all tracks
        stream.getTracks().forEach((track) => track.stop());
      };

      mediaRecorderRef.current = mediaRecorder;
      mediaRecorder.start();
      setIsRecording(true);
      setRecordingTime(0);

      // Start recording timer
      recordingIntervalRef.current = setInterval(() => {
        setRecordingTime((prev) => prev + 1);
      }, 1000);
    } catch (error) {
      console.error("Error starting recording:", error);
      alert("Could not access microphone. Please check permissions.");
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      setIsRecording(false);
      if (recordingIntervalRef.current) {
        clearInterval(recordingIntervalRef.current);
      }
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

  const uploadAudio = async (audioBlob, taskId) => {
    setUploading((prev) => ({ ...prev, [taskId]: true }));
    try {
      const formData = new FormData();

      // Xác định extension dựa trên MIME type
      let extension = "webm";
      if (audioBlob.type.includes("mp4")) extension = "mp4";
      else if (audioBlob.type.includes("wav")) extension = "wav";
      else if (audioBlob.type.includes("ogg")) extension = "ogg";

      formData.append(
        "file",
        audioBlob,
        `speaking_${taskId}_${Date.now()}.${extension}`
      );

      const response = await UploadApi.uploadAudio(audioBlob);
      setAudioUrls((prev) => ({
        ...prev,
        [taskId]: response.url,
      }));

      // No frontend transcription - will be done on backend during grading
      console.log(`Audio uploaded for task ${taskId}:`, response.url);
    } catch (error) {
      console.error("Upload failed:", error);
      alert("Failed to upload audio. Please try again.");
    } finally {
      setUploading((prev) => ({ ...prev, [taskId]: false }));
    }
  };

  // Transcription is now handled on backend during grading
  // No frontend transcription needed

  const handleNext = () => {
    if (currentIndex < tasks.length - 1) setCurrentIndex((i) => i + 1);
  };

  const handlePrev = () => {
    if (currentIndex > 0) setCurrentIndex((i) => i - 1);
  };

  const handleSubmit = async () => {
    // Phải có ít nhất 1 bản ghi
    if (!recordings || Object.keys(recordings).length === 0) {
      alert(
        "No valid recordings found. Please record your answer before submitting."
      );
      return;
    }

    setSubmitting(true);
    try {
      // 1) Xác định danh sách task cần nộp (full => tất cả, single => chỉ current)
      const speakingTasks =
        mode === "full" && Array.isArray(tasks) ? tasks : [currentTask];

      // 2) Build mảng answers: speakingId, displayOrder, audioUrl (Cloudinary)
      const answers = [];
      for (const t of speakingTasks) {
        const taskId = t.speakingId;
        const recordedBlob = recordings[taskId];

        // Bỏ qua câu không có ghi âm
        if (!recordedBlob && !audioUrls[taskId]) continue;

        let url = audioUrls[taskId];

        // Nếu đang là blob: thì upload để lấy link Cloudinary
        if (!url || url.startsWith("blob:")) {
          const formData = new FormData();

          // Xác định extension dựa trên MIME type
          let extension = "webm";
          if (recordedBlob.type.includes("mp4")) extension = "mp4";
          else if (recordedBlob.type.includes("wav")) extension = "wav";
          else if (recordedBlob.type.includes("ogg")) extension = "ogg";

          formData.append(
            "file",
            recordedBlob,
            `speaking_${taskId}_${Date.now()}.${extension}`
          );
          const res = await fetch("/api/upload/audio", {
            method: "POST",
            body: formData,
          });
          if (!res.ok) throw new Error("Upload audio failed");
          const data = await res.json();
          url = data.url;
        }

        answers.push({
          speakingId: taskId,
          displayOrder: t.displayOrder ?? 1,
          audioUrl: url,
          // transcript will be generated on backend
          transcript: "",
        });
      }

      if (answers.length === 0) {
        alert(
          "No valid recordings found. Please record your answer before submitting."
        );
        return;
      }

      // 3) Lấy userId (nếu backend cần)
      let userId = undefined;
      try {
        const user = JSON.parse(localStorage.getItem("user"));
        userId = user?.userId;
      } catch {}

      // 4) Payload đúng định dạng DTO backend
      const payload = {
        examId: exam.examId,
        userId, // có thể undefined nếu BE không bắt buộc
        mode, // "full" | "single"
        answers,
      };

      // 5) Gọi API chấm điểm
      const result = await SpeakingApi.gradeSpeaking(payload);

      // 6) Điều hướng sang trang kết quả giống luồng bạn đang dùng
      navigate("/speaking/result", {
        state: {
          examId: exam.examId,
          userId,
          exam,
          mode,
          recordings: audioUrls, // map speakingId -> cloudinary url
          isWaiting: true, // để trang result poll feedback, tùy flow của bạn
        },
      });
    } catch (err) {
      console.error("Submit failed:", err);
      alert("Failed to submit your test. Please try again.");
    } finally {
      setSubmitting(false);
    }
  };

  const hasRecording = recordings[currentId];
  const isUploading = uploading[currentId];
  const formatRecordingTime = (seconds) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, "0")}`;
  };

  return (
    <AppLayout title="Speaking Test" sidebar={<GeneralSidebar />}>
      <div className={styles.container}>
        <div className={styles.header}>
          <div className={styles.titleSection}>
            <Mic className={styles.titleIcon} />
            <h2>
              {mode === "full"
                ? `Full Speaking Test — ${exam.examName}`
                : `${currentTask?.speakingType} — ${exam.examName}`}
            </h2>
          </div>
          <div className={styles.timer}>
            <Clock size={20} /> {formatTime(timeLeft)}
          </div>
        </div>

        <div className={styles.mainContent}>
          {/* Question Panel */}
          <div className={styles.questionPanel}>
            <div className={styles.questionHeader}>
              <h3>Question {mode === "full" ? currentIndex + 1 : 1}</h3>
              <span className={styles.taskType}>
                {currentTask?.speakingType}
              </span>
            </div>
            <div className={styles.questionContent}>
              <p>{currentTask?.speakingQuestion}</p>
            </div>
            <div className={styles.instructions}>
              <AlertCircle size={16} />
              <span>
                Speak clearly and naturally. You can record multiple times
                before submitting.
              </span>
            </div>
          </div>

          {/* Recording Panel */}
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
                      setRecordings((prev) => {
                        const newRecordings = { ...prev };
                        delete newRecordings[currentId];
                        return newRecordings;
                      });
                      setAudioUrls((prev) => {
                        const newUrls = { ...prev };
                        delete newUrls[currentId];
                        return newUrls;
                      });
                      setTranscripts((prev) => {
                        const newTranscripts = { ...prev };
                        delete newTranscripts[currentId];
                        return newTranscripts;
                      });
                    }}
                  >
                    <RotateCcw size={20} />
                    Record Again
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
                  <span>Uploading and transcribing...</span>
                </div>
              )}
            </div>

            {/* Audio Player */}
            {hasRecording && (
              <div className={styles.audioPlayer}>
                <audio
                  ref={audioRef}
                  src={audioUrls[currentId]}
                  onEnded={() => setIsPlaying(false)}
                  onPlay={() => setIsPlaying(true)}
                  onPause={() => setIsPlaying(false)}
                />
                <div className={styles.audioInfo}>
                  <Volume2 size={16} />
                  <span>Your recording is ready</span>
                </div>
              </div>
            )}

            {/* Transcript Display */}
            {/* Transcript will be shown in result page after backend processing */}
          </div>

          {/* Navigation */}
          <div className={styles.navigation}>
            {mode === "full" && (
              <div className={styles.taskNavigation}>
                {currentIndex > 0 && (
                  <button onClick={handlePrev} className={styles.navBtn}>
                    ← Previous
                  </button>
                )}
                {currentIndex < tasks.length - 1 && (
                  <button onClick={handleNext} className={styles.navBtn}>
                    Next →
                  </button>
                )}
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
                    <div className={styles.spinner}></div>
                    Submitting...
                  </>
                ) : (
                  <>
                    <CheckCircle size={20} />
                    Submit Test
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

      {/* Loading Overlay */}
      {submitting && (
        <LoadingComponent text="Submitting your speaking test..." />
      )}
    </AppLayout>
  );
}
