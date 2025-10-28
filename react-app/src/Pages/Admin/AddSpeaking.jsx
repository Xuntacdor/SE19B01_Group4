import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import AdminNavbar from "../../Components/Admin/AdminNavbar";
import * as SpeakingApi from "../../Services/SpeakingApi";
import * as UploadApi from "../../Services/UploadApi";
import styles from "./AddSpeaking.module.css";
import {
  Mic,
  CheckCircle,
  XCircle,
  Upload,
  Image as ImageIcon,
  Pencil,
  PlusCircle,
  ArrowLeft,
  Clock,
  MessageCircle,
} from "lucide-react";

export default function AddSpeaking() {
  const location = useLocation();
  const navigate = useNavigate();
  const exam = location.state?.exam;
  const skill = location.state?.skill;

  const [form, setForm] = useState({
    examId: exam?.examId ?? exam?.ExamId ?? "",
    speakingType: "Part 1",
    displayOrder: 1,
    questions: [""], // nhiều câu hỏi trong cùng Part
    imageUrl: "",
    preparationTime: 60,
    speakingTime: 120,
    instructions: "",
  });

  const [status, setStatus] = useState({ icon: null, message: "" });
  const [uploading, setUploading] = useState(false);

  // Nếu có skill (edit mode)
  useEffect(() => {
    if (skill) {
      setForm({
        examId: skill.examId ?? exam?.examId,
        speakingType: skill.speakingType ?? "Part 1",
        displayOrder: skill.displayOrder ?? 1,
        questions: [skill.speakingQuestion ?? ""],
        imageUrl: skill.imageUrl ?? "",
        preparationTime: skill.preparationTime ?? 60,
        speakingTime: skill.speakingTime ?? 120,
        instructions: skill.instructions ?? "",
      });
    }
  }, [skill, exam]);

  // Xử lý thay đổi trong form
  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm({ ...form, [name]: value });
  };

  const handleSpeakingTypeChange = (e) => {
    const speakingType = e.target.value;
    let preparationTime = 60;
    let speakingTime = 120;
    let instructions = "";

    switch (speakingType) {
      case "Part 1":
        preparationTime = 0;
        speakingTime = 300;
        instructions =
          "Answer the questions naturally. Give detailed responses with examples.";
        break;
      case "Part 2":
        preparationTime = 60;
        speakingTime = 120;
        instructions =
          "You have 1 minute to prepare. Then speak for 1-2 minutes about the topic.";
        break;
      case "Part 3":
        preparationTime = 0;
        speakingTime = 300;
        instructions =
          "Discuss the topic in depth. Give your opinion and support it with examples.";
        break;
    }

    setForm({
      ...form,
      speakingType,
      preparationTime,
      speakingTime,
      instructions,
    });
  };

  // === MULTI QUESTION HANDLERS ===
  const addQuestion = () => {
    setForm({ ...form, questions: [...form.questions, ""] });
  };

  const removeQuestion = (index) => {
    setForm({
      ...form,
      questions: form.questions.filter((_, i) => i !== index),
    });
  };

  const handleQuestionChange = (index, value) => {
    const updated = [...form.questions];
    updated[index] = value;
    setForm({ ...form, questions: updated });
  };

  // Upload image
  const handleImageUpload = (e) => {
    const file = e.target.files[0];
    if (!file) return;
    setUploading(true);
    setStatus({ icon: <Upload size={16} />, message: "Uploading image..." });

    UploadApi.uploadImage(file)
      .then((res) => {
        setForm((prev) => ({ ...prev, imageUrl: res.url }));
        setStatus({
          icon: <CheckCircle color="green" size={16} />,
          message: "Image uploaded successfully!",
        });
      })
      .catch((err) => {
        console.error(err);
        setStatus({
          icon: <XCircle color="red" size={16} />,
          message: "Failed to upload image.",
        });
      })
      .finally(() => setUploading(false));
  };

  // Submit: gửi nhiều record cùng part
  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!form.examId) {
      setStatus({
        icon: <XCircle color="red" size={16} />,
        message: "Exam ID is missing.",
      });
      return;
    }

    if (form.questions.some((q) => !q.trim())) {
      setStatus({
        icon: <XCircle color="red" size={16} />,
        message: "Please fill all question fields.",
      });
      return;
    }

    setStatus({ icon: <Upload size={16} />, message: "Adding questions..." });

    try {
      // Gửi từng câu hỏi lên backend (mỗi câu là 1 record Speaking)
      for (let i = 0; i < form.questions.length; i++) {
        const payload = {
          examId: form.examId,
          speakingQuestion: form.questions[i],
          speakingType: form.speakingType,
          displayOrder: parseInt(form.displayOrder) + i, // tăng dần theo thứ tự câu
        };
        await SpeakingApi.add(payload);
      }

      setStatus({
        icon: <CheckCircle color="green" size={16} />,
        message: "All questions added successfully!",
      });

      setTimeout(() => navigate(-1), 1200);
    } catch (err) {
      console.error(err);
      setStatus({
        icon: <XCircle color="red" size={16} />,
        message: "Failed to add speaking questions.",
      });
    }
  };

  return (
    <div className={styles.splitLayout}>
      <AdminNavbar />

      {/* ===== Left panel ===== */}
      <div className={styles.leftPanel}>
        <h2>
          <Mic size={22} style={{ marginRight: 6 }} />
          <PlusCircle size={18} style={{ marginRight: 6 }} /> Add Speaking for{" "}
          {exam?.examName || exam?.ExamName}
        </h2>

        <p className={styles.examInfo}>
          <strong>Exam:</strong> {exam?.examName || exam?.ExamName} (
          {exam?.examType || exam?.ExamType})
        </p>

        <form onSubmit={handleSubmit} className={styles.form}>
          {/* Speaking Type */}
          <div className={styles.group}>
            <label>Speaking Part</label>
            <select
              name="speakingType"
              value={form.speakingType}
              onChange={handleSpeakingTypeChange}
              required
            >
              <option value="Part 1">Part 1 - Introduction & Interview</option>
              <option value="Part 2">Part 2 - Individual Long Turn</option>
              <option value="Part 3">Part 3 - Two-way Discussion</option>
            </select>
          </div>

          {/* Display Order */}
          <div className={styles.group}>
            <label>Display Order (base)</label>
            <input
              type="number"
              name="displayOrder"
              value={form.displayOrder}
              onChange={handleChange}
              placeholder="Starting order number (e.g. 1)"
              required
            />
            <small className={styles.note}>
              Questions will auto-increment display order starting from this
              number
            </small>
          </div>

          {/* Instructions */}
          <div className={styles.group}>
            <label>Instructions</label>
            <textarea
              name="instructions"
              value={form.instructions}
              onChange={handleChange}
              placeholder="Instructions for the speaking task..."
              rows={3}
              required
            />
          </div>

          {/* MULTI QUESTIONS */}
          <div className={styles.group}>
            <label>Questions</label>
            {form.questions.map((q, i) => (
              <div key={i} className={styles.multiQuestionRow}>
                <textarea
                  value={q}
                  onChange={(e) => handleQuestionChange(i, e.target.value)}
                  placeholder={`Question ${i + 1}`}
                  rows={2}
                  required
                />
                {form.questions.length > 1 && (
                  <button
                    type="button"
                    onClick={() => removeQuestion(i)}
                    className={styles.removeBtn}
                  >
                    ✕
                  </button>
                )}
              </div>
            ))}
            <button
              type="button"
              onClick={addQuestion}
              className={styles.addBtn}
            >
              + Add Another Question
            </button>
          </div>

          {/* Upload & Submit */}
          <div className={styles.actionRow}>
            <div className={styles.uploadBox}>
              <input
                type="file"
                id="upload"
                accept="image/*"
                onChange={handleImageUpload}
                disabled={uploading}
              />
              <label htmlFor="upload" className={styles.uploadLabel}>
                <Upload size={18} />
                {uploading ? "Uploading..." : "Choose Image"}
              </label>
            </div>

            <button type="submit" className={styles.btnPrimary}>
              <PlusCircle size={16} style={{ marginRight: 6 }} /> Add All
              Questions
            </button>
          </div>
        </form>

        {status.message && (
          <p className={styles.status}>
            {status.icon}
            <span style={{ marginLeft: 6 }}>{status.message}</span>
          </p>
        )}

        <button className={styles.backBtn} onClick={() => navigate(-1)}>
          <ArrowLeft size={16} style={{ marginRight: 6 }} /> Back
        </button>
      </div>

      {/* ===== Right panel (Preview) ===== */}
      <div className={styles.rightPanel}>
        <h3>
          <ImageIcon size={18} style={{ marginRight: 6 }} />
          Preview
        </h3>

        <div className={styles.previewCard}>
          <div className={styles.previewHeader}>
            <Mic size={20} />
            <span>IELTS Speaking {form.speakingType}</span>
          </div>

          <div className={styles.previewContent}>
            <h4>Instructions</h4>
            <p>{form.instructions || "(Instructions will appear here)"}</p>

            <h4>Questions</h4>
            {form.questions.map((q, i) => (
              <p key={i}>• {q || "(Empty question)"} </p>
            ))}
          </div>
        </div>

        {form.imageUrl ? (
          <img
            src={form.imageUrl}
            alt="Preview"
            className={styles.previewImage}
          />
        ) : (
          <p className={styles.placeholder}>No image uploaded yet.</p>
        )}
      </div>
    </div>
  );
}
