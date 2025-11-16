import React, { useState, useEffect, useRef } from "react";
import { useLocation, useSearchParams } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as SpeakingApi from "../../Services/SpeakingApi";
import * as UploadApi from "../../Services/UploadApi";
import LoadingComponent from "../../Components/Exam/LoadingComponent";
import MicroCheck from "../../Components/Exam/MicroCheck";
// import FloatingDictionaryChat from "../../Components/Dictionary/FloatingDictionaryChat";
import { getSpeakingSuggestion } from "../../Services/SpeakingApi";
import VocabularySuggestion from "../../Components/Exam/VocabularySuggestion";
import SampleAnswerBox from "../../Components/Exam/SampleAnswerBox";
import AudioTextBox from "../../Components/Common/AudioTextBox";
import {
  Mic,
  MicOff,
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
  const [speakLeft, setSpeakLeft] = useState(60);
  const [recordings, setRecordings] = useState({});
  const [audioUrls, setAudioUrls] = useState({});
  const [uploading, setUploading] = useState({});
  const [speakingTask, setSpeakingTask] = useState(null);
  const [note, setNote] = useState("");
  const [feedback, setFeedback] = useState(null);
  const [showPrepPopup, setShowPrepPopup] = useState(false);
  const [chatMessages, setChatMessages] = useState([]);
  const [chatLoading, setChatLoading] = useState(false);

  // ===== Navigation state =====
  const [currentIndex, setCurrentIndex] = useState(0);
  const [currentPart, setCurrentPart] = useState("");

  const mediaRecorderRef = useRef(null);
  const speakTimerRef = useRef(null);
  const hasSubmittedRef = useRef(false);

  const [params] = useSearchParams();
  const speakingId = params.get("speakingId");
  const chatEndRef = useRef(null);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [chatMessages]);

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

  const { exam, tasks, task, mode } = state || {};
  const currentExam =
    exam ||
    (speakingTask
      ? { examId: speakingTask.examId, examName: "Single Speaking Task" }
      : undefined);
  const currentMode = mode || (speakingTask ? "single" : undefined);
  const orderedTasks = React.useMemo(() => {
    if (!Array.isArray(tasks)) return [];
    const order = { "Part 1": 1, "Part 2": 2, "Part 3": 3 };
    return [...tasks].sort((a, b) => {
      const pa = order[a.speakingType] ?? 99;
      const pb = order[b.speakingType] ?? 99;
      if (pa !== pb) return pa - pb;
      const da = a.displayOrder ?? a.DisplayOrder ?? 0;
      const db = b.displayOrder ?? b.DisplayOrder ?? 0;
      return da - db;
    });
  }, [tasks]);

  const currentTask =
    currentMode === "full" || currentMode === "part"
      ? orderedTasks[currentIndex]
      : task || (Array.isArray(tasks) ? tasks[0] : speakingTask);
  const currentId =
    currentMode === "full" || currentMode === "part"
      ? currentTask?.speakingId
      : task?.speakingId ||
        (Array.isArray(tasks)
          ? tasks[0]?.speakingId
          : speakingTask?.speakingId);

  // ===== Update currentPart =====
  useEffect(() => {
    if (currentMode === "full" && Array.isArray(tasks)) {
      const part = tasks[currentIndex]?.speakingType || "";
      setCurrentPart(part);
    } else {
      setCurrentPart(currentTask?.speakingType || "");
    }
  }, [currentIndex, currentTask, currentMode, tasks]);

  // ===== Load Existing Feedback =====
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
          console.log(" Existing feedback found:", res.feedback);
          setFeedback(res.feedback);
          setPhase("result");
        }
      } catch {
        console.warn(" No previous feedback found or fetch failed");
      }
    };

    fetchExistingFeedback();
  }, [currentId]);
  // Soft reset when switching question
  useEffect(() => {
    setPhase("idle");
    setFeedback(null);
    hasSubmittedRef.current = false;
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
        setUploading((p) => ({ ...p, [currentId]: true }));

        try {
          const res = await UploadApi.uploadAudio(blob);
          const audioUrl =
            res?.url || res?.path || (typeof res === "string" ? res : null);
          if (!audioUrl) throw new Error("No audio URL returned");

          setRecordings((p) => ({ ...p, [currentId]: blob }));
          setAudioUrls((p) => ({ ...p, [currentId]: audioUrl }));
          console.log(" Uploaded audio:", audioUrl);
        } catch (err) {
          console.error(" Upload failed:", err);
          alert("Upload failed, please try again.");
        } finally {
          setUploading((p) => ({ ...p, [currentId]: false }));
          stream.getTracks().forEach((t) => t.stop());
        }
      };

      mediaRecorder.start();
      mediaRecorderRef.current = mediaRecorder;
    } catch (err) {
      alert("Cannot access microphone!");
      console.error(" Microphone error:", err);
      setPhase("idle");
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current?.state === "recording") {
      mediaRecorderRef.current.stop();
    }
  };

  const speakQuestion = (text) => {
    if (!text) return;
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = "en-US";
    utterance.rate = 1;
    utterance.pitch = 1;
    window.speechSynthesis.cancel();
    window.speechSynthesis.speak(utterance);
  };

  useEffect(() => {
    if (
      audioUrls[currentId] &&
      !hasSubmittedRef.current &&
      !uploading[currentId]
    ) {
      hasSubmittedRef.current = true;
      handleSubmit();
    }
  }, [audioUrls, currentId, uploading]);

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
      console.error(" Submit failed:", err);
      setPhase("idle");
    }
  };

  const beginFlow = () => {
    if (phase !== "idle") return;

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

  // ===== Navigation Bar =====
  const renderNavigationBar = () => {
    if (!orderedTasks || orderedTasks.length === 0) return null;
    const partOrder = { "Part 1": 1, "Part 2": 2, "Part 3": 3 };
    const currentTaskObj = orderedTasks[currentIndex];
    const partGroups = React.useMemo(() => {
      const g = {};
      for (const t of orderedTasks) {
        const part = t.speakingType || "Unknown";
        if (!g[part]) g[part] = [];
        g[part].push(t);
      }
      return g;
    }, [orderedTasks]);

    const currentPartName = currentTaskObj?.speakingType || "Unknown";
    const currentPartTasks = partGroups[currentPartName] || [];
    const partIndex =
      currentPartTasks.findIndex(
        (t) => t.speakingId === currentTaskObj?.speakingId
      ) + 1;

    const handleNext = () => {
      if (currentIndex < orderedTasks.length - 1)
        setCurrentIndex(currentIndex + 1);
    };
    const handlePrev = () => {
      if (currentIndex > 0) setCurrentIndex(currentIndex - 1);
    };

    return (
      <div className={styles.navBar}>
        {(currentMode === "full" || currentMode === "part") && (
          <div className={styles.partTabs}>
            {Object.keys(partGroups)
              .sort((a, b) => (partOrder[a] || 99) - (partOrder[b] || 99))
              .map((part) => (
                <button
                  key={part}
                  className={`${styles.partTab} ${
                    currentPartName === part ? styles.activePart : ""
                  }`}
                  onClick={() => {
                    const firstIndex = orderedTasks.findIndex(
                      (t) => t.speakingType === part
                    );
                    if (firstIndex !== -1) setCurrentIndex(firstIndex);
                  }}
                >
                  {part}
                </button>
              ))}
          </div>
        )}

        <div className={styles.navControls}>
          <button
            className={styles.navBtn}
            onClick={handlePrev}
            disabled={currentIndex === 0}
          >
            ← Prev
          </button>

          <span className={styles.navInfo}>
            {`${currentPartName} — Question ${partIndex} of ${currentPartTasks.length}`}
          </span>

          <button
            className={styles.navBtn}
            onClick={handleNext}
            disabled={currentIndex === orderedTasks.length - 1}
          >
            Next →
          </button>
        </div>
      </div>
    );
  };

  if (!currentTask)
    return (
      <AppLayout title="Speaking Test" sidebar={<GeneralSidebar />}>
        <div className={styles.center}>
          <LoadingComponent text="Loading speaking test..." />
        </div>
      </AppLayout>
    );

  const renderDynamicSection = () => {
    switch (phase) {
      case "idle":
        return (
          <button className={styles.recordBtn} onClick={beginFlow}>
            <Mic size={24} /> Start Recording
          </button>
        );
      case "recording":
        return (
          <button
            className={`${styles.recording} ${styles.recordBtn}`}
            onClick={() => {
              clearInterval(speakTimerRef.current);
              stopRecording();
              setPhase("done");
            }}
          >
            <MicOff size={24} /> Recording... ({speakLeft}s)
          </button>
        );
      case "analyzing":
        return <LoadingComponent text="Analyzing your answer..." />;
      case "waiting":
        return <LoadingComponent text="Waiting for AI feedback..." />;
      case "result":
        return (
          feedback && (
            <div className={styles.resultBox}>
              <div className={styles.resultHeader}>
                <div className={styles.resultLeft}>
                  <Trophy size={28} color="#2563eb" />
                  <h3>Speaking Result</h3>
                </div>

                <button
                  className={styles.retakeBtn}
                  onClick={() => {
                    setPhase("idle");
                    hasSubmittedRef.current = false;
                    setFeedback(null);
                  }}
                >
                  <RotateCcw size={18} /> Retake
                </button>
              </div>
              <div className={styles.resultBody}>
                <div className={styles.userAnswerSection}>
                  <h4>Your Answer</h4>
                  {feedback.audioUrl && (
                    <div className={styles.audioPlayer}>
                      <audio controls src={feedback.audioUrl} />
                    </div>
                  )}
                  <p className={styles.transcriptText}>
                    {feedback.transcript || "Transcript not available yet."}
                  </p>
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

                {feedback?.aiAnalysisJson &&
                  (() => {
                    const ai =
                      JSON.parse(feedback.aiAnalysisJson)?.ai_analysis || {};

                    return (
                      <div className={styles.aiSection}>
                        <h4>AI Feedback</h4>

                        {/* Overview */}
                        {ai.overview && (
                          <div className={styles.aiOverview}>{ai.overview}</div>
                        )}

                        {/* Strengths */}
                        {ai.strengths?.length > 0 && (
                          <>
                            <div className={styles.aiSubTitle}>Strengths:</div>
                            <ul className={styles.aiList}>
                              {ai.strengths.map((s, i) => (
                                <li key={i}>{s}</li>
                              ))}
                            </ul>
                          </>
                        )}

                        {/* Weaknesses */}
                        {ai.weaknesses?.length > 0 && (
                          <>
                            <div className={styles.aiSubTitle}>Weaknesses:</div>
                            <ul className={styles.aiList}>
                              {ai.weaknesses.map((w, i) => (
                                <li key={i}>{w}</li>
                              ))}
                            </ul>
                          </>
                        )}

                        {/* Advice */}
                        {ai.advice && (
                          <div className={styles.aiAdviceBox}>
                            <strong>Advice:</strong> {ai.advice}
                          </div>
                        )}

                        {/* Vocabulary Suggestions */}
                        {ai.vocabulary_suggestions?.length > 0 && (
                          <div className={styles.vocabSuggestBox}>
                            <h4>Vocabulary Suggestions</h4>

                            {ai.vocabulary_suggestions.map((v, i) => (
                              <div key={i} className={styles.vocabItem}>
                                <strong>{v.original_word}</strong> →{" "}
                                {v.suggested_alternative}
                                <p className={styles.vocabExplain}>
                                  {v.explanation}
                                </p>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    );
                  })()}
              </div>
            </div>
          )
        );
      case "done":
        return (
          hasRecording && (
            <>
              <div className={styles.audioPlayer}>
                <audio controls src={audioUrls[currentId]} />
              </div>
              <div className={styles.audioInfo}>
                <Volume2 size={16} /> <span>Your recording is ready</span>
              </div>
            </>
          )
        );
      default:
        return null;
    }
  };

  return (
    <div style={{ display: "flex", height: "100vh" }}>
      <div style={{ width: "250px" }}>
        <GeneralSidebar />
      </div>

      <main style={{ flex: 1, padding: "20px" }}>
        <div className={styles.container}>
          <div className={styles.header}>
            <div className={styles.titleSection}>
              <Mic className={styles.titleIcon} />
              <h2>
                {currentTask?.speakingType} — {currentExam?.examName}
              </h2>
            </div>
            <MicroCheck />
            <div className={styles.timer}>
              <Clock size={20} /> {speakLeft > 0 ? `${speakLeft}s` : "00:00"}
            </div>
          </div>

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
              <div className={styles.questionHeader}>
                <h3>Question</h3>
              </div>
              <AudioTextBox
                //label="Question"
                text={currentTask?.speakingQuestion}
              />

              <div className={styles.dynamicSection}>
                {renderDynamicSection()}
              </div>

              {(currentMode === "full" || currentMode === "part") &&
                renderNavigationBar()}
            </div>

            <aside className={styles.chatPanel}>
              <h4>Speaking Assistant</h4>

              <div className={styles.chatBox}>
                {chatMessages.length === 0 ? (
                  <p className={styles.chatPlaceholder}>
                    Hello! I can help you practice your speaking. Choose one of
                    the options below to get started.
                  </p>
                ) : (
                  chatMessages.map((msg, idx) => (
                    <div
                      key={idx}
                      className={`${styles.chatMessage} ${
                        msg.role === "user" ? styles.userMsg : styles.botMsg
                      }`}
                    >
                      {msg.text && <div>{msg.text}</div>}
                      {msg.component && msg.component}
                    </div>
                  ))
                )}

                {chatLoading && (
                  <p className={styles.chatTyping}>AI is typing....</p>
                )}
                <div ref={chatEndRef}></div>
              </div>

              <div className={styles.chatActions}>
                <button
                  className={styles.chatBtn}
                  onClick={async () => {
                    if (!currentId) return;
                    setChatLoading(true);
                    setChatMessages((prev) => [
                      ...prev,
                      { role: "user", text: "Show me a sample answer" },
                    ]);
                    try {
                      const res = await getSpeakingSuggestion(currentId);
                      setChatMessages((prev) => [
                        ...prev,
                        {
                          role: "bot",
                          component: (
                            <SampleAnswerBox
                              text={
                                res.sample_answer ||
                                "Không tìm thấy câu mẫu cho câu hỏi này."
                              }
                            />
                          ),
                        },
                      ]);
                    } catch (err) {
                      setChatMessages((prev) => [
                        ...prev,
                        {
                          role: "bot",
                          text: " Error fetching sample answer. Please try again.",
                        },
                      ]);
                    } finally {
                      setChatLoading(false);
                    }
                  }}
                >
                  Sample Answer
                </button>

                <button
                  className={styles.chatBtn}
                  onClick={async () => {
                    if (!currentId) return;
                    setChatLoading(true);
                    setChatMessages((prev) => [
                      ...prev,
                      { role: "user", text: "Show me topic vocabulary" },
                    ]);
                    try {
                      const res = await getSpeakingSuggestion(currentId);
                      const vocab = res.vocabulary_by_level;
                      setChatMessages((prev) => [
                        ...prev,
                        {
                          role: "bot",
                          component: <VocabularySuggestion vocab={vocab} />,
                        },
                      ]);
                    } catch {
                      setChatMessages((prev) => [
                        ...prev,
                        {
                          role: "bot",
                          text: " Lỗi khi lấy từ vựng, thử lại sau nhé.",
                        },
                      ]);
                    } finally {
                      setChatLoading(false);
                    }
                  }}
                >
                  Topic Vocabulary
                </button>
              </div>
            </aside>
          </div>
        </div>
        {/* <FloatingDictionaryChat /> */}
      </main>
    </div>
  );
}
