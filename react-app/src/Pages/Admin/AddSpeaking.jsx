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
  Users,
} from "lucide-react";

export default function AddSpeaking() {
  const location = useLocation();
  const navigate = useNavigate();
  const exam = location.state?.exam;
  const skill = location.state?.skill;

  const [form, setForm] = useState({
    examId: exam?.examId ?? exam?.ExamId ?? "",
    speakingQuestion: "",
    speakingType: "Part 1", // Part 1, Part 2, Part 3
    displayOrder: "",
    imageUrl: "",
    preparationTime: 60, // seconds for Part 2
    speakingTime: 120, // seconds for Part 2
    instructions: "",
  });

  const [status, setStatus] = useState({ icon: null, message: "" });
  const [uploading, setUploading] = useState(false);

  // === Nếu có skill (edit mode) thì load dữ liệu ===
  useEffect(() => {
    if (skill) {
      setForm({
        examId: skill.examId ?? exam?.examId,
        speakingQuestion: skill.speakingQuestion ?? "",
        speakingType: skill.speakingType ?? "Part 1",
        displayOrder: skill.displayOrder ?? 1,
        imageUrl: skill.imageUrl ?? "",
        preparationTime: skill.preparationTime ?? 60,
        speakingTime: skill.speakingTime ?? 120,
        instructions: skill.instructions ?? "",
      });
    }
  }, [skill, exam]);

  // === Handle input change ===
  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm({ ...form, [name]: value });
  };

  // === Handle speaking type change ===
  const handleSpeakingTypeChange = (e) => {
    const speakingType = e.target.value;
    let preparationTime = 60;
    let speakingTime = 120;
    let instructions = "";

    switch (speakingType) {
      case "Part 1":
        preparationTime = 0;
        speakingTime = 300; // 5 minutes
        instructions = "Answer the questions naturally. Give detailed responses with examples.";
        break;
      case "Part 2":
        preparationTime = 60;
        speakingTime = 120; // 2 minutes
        instructions = "You have 1 minute to prepare. Then speak for 1-2 minutes about the topic.";
        break;
      case "Part 3":
        preparationTime = 0;
        speakingTime = 300; // 5 minutes
        instructions = "Discuss the topic in depth. Give your opinion and support it with examples.";
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

  // === Upload image ===
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

  // === Submit form ===
  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!form.examId) {
      setStatus({
        icon: <XCircle color="red" size={16} />,
        message: "Exam ID is missing.",
      });
      return;
    }

    if (!form.displayOrder) {
      setStatus({
        icon: <XCircle color="red" size={16} />,
        message: "Please enter display order.",
      });
      return;
    }

    if (!form.speakingQuestion.trim()) {
      setStatus({
        icon: <XCircle color="red" size={16} />,
        message: "Please enter speaking question content.",
      });
      return;
    }

    setStatus({ icon: <Upload size={16} />, message: "Processing..." });

    try {
      // Only send fields that the backend currently supports
      const payload = {
        examId: form.examId,
        speakingQuestion: form.speakingQuestion,
        speakingType: form.speakingType,
        displayOrder: parseInt(form.displayOrder),
        // Note: imageUrl, preparationTime, speakingTime, instructions will be added to backend later
      };

      if (skill) {
        await SpeakingApi.update(skill.speakingId, payload);
        setStatus({
          icon: <CheckCircle color="green" size={16} />,
          message: "Updated speaking question successfully!",
        });
      } else {
        await SpeakingApi.add(payload);
        setStatus({
          icon: <CheckCircle color="green" size={16} />,
          message: "Speaking question added successfully!",
        });
      }

      setTimeout(() => navigate(-1), 1000);
    } catch (err) {
      console.error(err);
      setStatus({
        icon: <XCircle color="red" size={16} />,
        message: "Failed to save speaking question.",
      });
    }
  };

  // === Get sample questions based on speaking type ===
  const getSampleQuestions = (speakingType) => {
    switch (speakingType) {
      case "Part 1":
        return [
          "What do you do for work?",
          "Do you enjoy your job? Why or why not?",
          "What do you like to do in your free time?",
          "Tell me about your hometown.",
          "Do you prefer living in the city or countryside?",
        ];
      case "Part 2":
        return [
          "Describe a memorable trip you have taken. You should say:\n- where you went\n- who you went with\n- what you did there\n- and explain why it was memorable",
          "Describe a person who has influenced you. You should say:\n- who this person is\n- how you know them\n- what they have done\n- and explain why they influenced you",
          "Describe your favorite book or movie. You should say:\n- what it is\n- what it is about\n- when you first experienced it\n- and explain why you like it",
        ];
      case "Part 3":
        return [
          "What are the advantages and disadvantages of living in a big city?",
          "How has technology changed the way people communicate?",
          "Do you think it's important for children to learn a second language? Why?",
          "What role does education play in society?",
          "How do you think the workplace will change in the future?",
        ];
      default:
        return [];
    }
  };

  return (
    <div className={styles.splitLayout}>
      <AdminNavbar />

      {/* ===== Left panel ===== */}
      <div className={styles.leftPanel}>
        <h2>
          <Mic size={22} style={{ marginRight: 6 }} />
          {skill ? (
            <>
              <Pencil size={18} style={{ marginRight: 6 }} /> Edit Speaking for{" "}
              {exam?.examName || exam?.ExamName}
            </>
          ) : (
            <>
              <PlusCircle size={18} style={{ marginRight: 6 }} /> Add Speaking for{" "}
              {exam?.examName || exam?.ExamName}
            </>
          )}
        </h2>

        {exam && (
          <p className={styles.examInfo}>
            <strong>Exam:</strong> {exam.examName || exam.ExamName} (
            {exam.examType || exam.ExamType})
          </p>
        )}

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
              <option value="Part 1">Part 1 - Introduction & Interview (4-5 min)</option>
              <option value="Part 2">Part 2 - Individual Long Turn (3-4 min)</option>
              <option value="Part 3">Part 3 - Two-way Discussion (4-5 min)</option>
            </select>
            <small className={styles.note}>
              Choose the appropriate IELTS Speaking part
            </small>
          </div>

          {/* Display Order */}
          <div className={styles.group}>
            <label>Display Order</label>
            <input
              type="number"
              name="displayOrder"
              value={form.displayOrder}
              onChange={handleChange}
              placeholder="1 for Part 1, 2 for Part 2, 3 for Part 3"
              min={1}
              max={3}
              required
            />
            <small className={styles.note}>
              Determines part order (1 = Part 1, 2 = Part 2, 3 = Part 3)
            </small>
          </div>

          {/* Timing Information */}
          <div className={styles.timingInfo}>
            <div className={styles.timingCard}>
              <Clock size={16} />
              <span>Preparation: {form.preparationTime}s</span>
            </div>
            <div className={styles.timingCard}>
              <MessageCircle size={16} />
              <span>Speaking: {form.speakingTime}s</span>
            </div>
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
            <small className={styles.note}>
              Instructions will be shown to the student before they start speaking
            </small>
          </div>

          {/* Speaking Question */}
          <div className={styles.group}>
            <label>Question Content</label>
            <textarea
              name="speakingQuestion"
              value={form.speakingQuestion}
              onChange={handleChange}
              placeholder="Enter speaking question or topic..."
              rows={8}
              required
            />
            <small className={styles.note}>
              For Part 2, include bullet points for what to cover
            </small>
          </div>

          {/* Sample Questions */}
          <div className={styles.sampleSection}>
            <h4>Sample Questions for {form.speakingType}</h4>
            <div className={styles.sampleQuestions}>
              {getSampleQuestions(form.speakingType).map((question, index) => (
                <div
                  key={index}
                  className={styles.sampleQuestion}
                  onClick={() => setForm({ ...form, speakingQuestion: question })}
                >
                  {question}
                </div>
              ))}
            </div>
          </div>

          {/* Upload + Submit cùng hàng */}
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
              {skill ? (
                <>
                  <Pencil size={16} style={{ marginRight: 6 }} /> Update Speaking
                </>
              ) : (
                <>
                  <PlusCircle size={16} style={{ marginRight: 6 }} /> Add Speaking
                </>
              )}
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

      {/* ===== Right panel: Preview ===== */}
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
            
            <h4>Question</h4>
            <p>{form.speakingQuestion || "(Question content will appear here)"}</p>
            
            <div className={styles.previewTiming}>
              <div className={styles.timingItem}>
                <Clock size={14} />
                <span>Prep: {form.preparationTime}s</span>
              </div>
              <div className={styles.timingItem}>
                <MessageCircle size={14} />
                <span>Speak: {form.speakingTime}s</span>
              </div>
            </div>
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
