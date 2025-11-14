import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import * as listeningService from "../../Services/ListeningApi";
import * as UploadApi from "../../Services/UploadApi";
import ExamMarkdownRenderer, {
  renderMarkdownToHtmlAndAnswers,
} from "../../Components/Exam/ExamMarkdownRenderer.jsx";
import styles from "./AddListening.module.css";
import {
  Music,
  Upload,
  Pencil,
  PlusCircle,
  CheckCircle,
  XCircle,
  ArrowLeft,
  Eye,
  EyeOff,
  FileAudio,
} from "lucide-react";

export default function AddListening() {
  const location = useLocation();
  const navigate = useNavigate();
  const exam = location.state?.exam;
  const skill = location.state?.skill;

  const [listeningContent, setListeningContent] = useState("");
  const [transcript, setTranscript] = useState("");          // 🔹 NEW
  const [listeningQuestion, setListeningQuestion] = useState("");
  const [status, setStatus] = useState({ icon: null, message: "" });
  const [showAnswers, setShowAnswers] = useState(true);
  const [uploading, setUploading] = useState(false);

  useEffect(() => {
    if (skill) {
      setListeningContent(skill.listeningContent || "");
      setListeningQuestion(skill.listeningQuestion || "");
      setTranscript(skill.transcript || "");                // 🔹 Prefill when editing
    }
  }, [skill]);

  // AddListening.jsx
  const handleUploadAudio = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const okTypes = new Set([
      "audio/mpeg",
      "audio/mp3",
      "audio/wav",
      "audio/x-wav",
      "audio/ogg",
    ]);
    const okExts = [".mp3", ".wav", ".ogg"];
    const hasDot = file.name.lastIndexOf(".") >= 0;
    const ext = hasDot
      ? file.name.slice(file.name.lastIndexOf(".")).toLowerCase()
      : "";

    if (
      file.name.startsWith("._") ||
      (!okTypes.has(file.type) && !okExts.includes(ext))
    ) {
      setStatus({
        icon: <XCircle color="red" size={16} />,
        message: "Unsupported audio type. Use MP3, WAV, or OGG.",
      });
      e.target.value = "";
      return;
    }

    setUploading(true);
    setStatus({ icon: <Upload size={16} />, message: "Uploading audio..." });

    try {
      const res = await UploadApi.uploadAudio(file);
      const audioUrl =
        res?.url || res?.path || (typeof res === "string" ? res : null);
      if (!audioUrl) throw new Error("No audio URL returned");

      setListeningContent(audioUrl);
      setStatus({
        icon: <CheckCircle color="green" size={16} />,
        message: "Audio uploaded successfully!",
      });
    } catch (err) {
      console.error("Upload failed:", err);
      setStatus({
        icon: <XCircle color="red" size={16} />,
        message: String(
          err?.response?.data || err?.message || "Failed to upload audio."
        ),
      });
    } finally {
      setUploading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setStatus({ icon: <FileAudio size={16} />, message: "Processing..." });

    try {
      // 1️⃣ First render without listeningId (uses "X_q#" keys)
      const { html, answers } = renderMarkdownToHtmlAndAnswers(
        listeningQuestion
      );

      const payload = {
        examId: exam.examId,
        listeningContent,
        transcript,                           // 🔹 send transcript to backend
        listeningQuestion,
        listeningType: "Markdown",
        displayOrder: skill?.displayOrder || 1,
        correctAnswer: JSON.stringify(answers),
        questionHtml: html,
      };

      let saved;
      if (skill) {
        // Update existing listening
        await listeningService.update(skill.listeningId, payload);
        saved = { listeningId: skill.listeningId };
        setStatus({
          icon: <CheckCircle color="green" size={16} />,
          message: "Updated successfully!",
        });
      } else {
        // Add new listening (get real listeningId)
        saved = await listeningService.add(payload);
        setStatus({
          icon: <CheckCircle color="green" size={16} />,
          message: "Added successfully!",
        });
      }

      // 2️⃣ Regenerate BOTH HTML + correct answers using real listeningId
      const newId = saved?.listeningId;
      if (newId) {
        const { html: fixedHtml, answers: fixedAnswers } =
          renderMarkdownToHtmlAndAnswers(listeningQuestion, newId);

        await listeningService.update(newId, {
          ...payload,                          // 🔹 still includes transcript
          questionHtml: fixedHtml,
          correctAnswer: JSON.stringify(fixedAnswers),
        });
      }

      setTimeout(() => navigate(-1), 1000);
    } catch (err) {
      console.error(err);
      setStatus({
        icon: <XCircle color="red" size={16} />,
        message: "Failed to save listening question.",
      });
    }
  };

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <h2>
          <Music size={22} style={{ marginRight: 6 }} />
          {skill ? (
            <>
              <Pencil size={18} style={{ marginRight: 6 }} />
              Edit Listening for {exam?.examName}
            </>
          ) : (
            <>
              <PlusCircle size={18} style={{ marginRight: 6 }} />
              Add Listening for {exam?.examName}
            </>
          )}
        </h2>
      </header>

      <div className={styles.grid}>
        <form onSubmit={handleSubmit} className={styles.form}>
          {/* ===== Audio upload ===== */}
          <div className={styles.group}>
            <label>Upload Audio File</label>

            <label className={styles.uploadBox}>
              <Upload size={24} className={styles.uploadIcon} />
              <span className={styles.uploadText}>
                {uploading
                  ? "Uploading..."
                  : "Click to upload or drag & drop audio file"}
              </span>
              <input
                type="file"
                accept=".mp3,.wav,.ogg,audio/mpeg,audio/mp3,audio/wav,audio/ogg"
                onChange={handleUploadAudio}
                disabled={uploading}
              />
            </label>

            {listeningContent && (
              <audio
                controls
                src={listeningContent}
                className={styles.audioPreview}
              />
            )}
          </div>

          {/* ===== Transcript (like reading content) ===== */}
          <div className={styles.group}>
            <label>Transcript / Script</label>
            <textarea
              value={transcript}
              onChange={(e) => setTranscript(e.target.value)}
              rows={6}
              placeholder="Paste or type the listening transcript here..."
            />
          </div>

          {/* ===== Question markdown ===== */}
          <div className={styles.group}>
            <label>Question (Markdown)</label>
            <textarea
              value={listeningQuestion}
              onChange={(e) => setListeningQuestion(e.target.value)}
              rows={10}
              placeholder="[!num] Question text here..."
            />
          </div>

          <div className={styles.buttons}>
            <button
              type="submit"
              className={styles.btnPrimary}
              disabled={
                uploading || !listeningContent || !listeningQuestion.trim()
              }
              title={
                uploading
                  ? "Please wait for the upload to finish"
                  : !listeningContent
                  ? "Upload an audio file first"
                  : !listeningQuestion.trim()
                  ? "Enter the question first"
                  : ""
              }
            >
              {skill ? (
                <>
                  <Pencil size={16} style={{ marginRight: 6 }} />
                  Update Question
                </>
              ) : (
                <>
                  <PlusCircle size={16} style={{ marginRight: 6 }} />
                  Add Question
                </>
              )}
            </button>

            <button
              type="button"
              className={styles.btnSecondary}
              onClick={() => navigate(-1)}
            >
              <ArrowLeft size={16} style={{ marginRight: 6 }} />
              Back
            </button>
          </div>

          {status.message && (
            <p className={styles.status}>
              {status.icon}
              <span style={{ marginLeft: 6 }}>{status.message}</span>
            </p>
          )}
        </form>

        <div className={styles.preview}>
          <div className={styles.previewHeader}>
            <h3>Live Preview</h3>
            <button
              onClick={() => setShowAnswers((v) => !v)}
              className={`${styles.btnToggle} ${
                showAnswers ? styles.show : styles.hide
              }`}
            >
              {showAnswers ? (
                <>
                  <EyeOff size={14} style={{ marginRight: 4 }} />
                  Hide Answers
                </>
              ) : (
                <>
                  <Eye size={14} style={{ marginRight: 4 }} />
                  Show Answers
                </>
              )}
            </button>
          </div>

          <div className={styles.previewBox}>
            {listeningQuestion ? (
              <ExamMarkdownRenderer
                markdown={listeningQuestion}
                showAnswers={showAnswers}
              />
            ) : (
              <p className={styles.placeholder}>
                Type markdown question to preview...
              </p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
