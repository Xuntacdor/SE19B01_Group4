import PopupBase from "../Common/PopupBase";
import React, { useState, useEffect } from "react";
import styles from "./ExamPopup.module.css";
import * as readingService from "../../Services/ReadingApi";
import * as listeningService from "../../Services/ListeningApi";
import * as writingService from "../../Services/WritingApi";
import * as speakingService from "../../Services/SpeakingApi";
import * as UploadApi from "../../Services/UploadApi";
import { Pencil, Trash2, PlusCircle, Loader2, BookOpen, GripVertical } from "lucide-react";

export default function ExamManageSkill({
  show,
  exam,
  onClose,
  onEdit,
  onDelete,
  onAddSkill,
  onUpdateExam,
}) {
  const [skills, setSkills] = useState([]);
  const [loading, setLoading] = useState(true);
  const [dragIndex, setDragIndex] = useState(null);

  // Editable exam fields
  const [examName, setExamName] = useState("");
  const [examType, setExamType] = useState("");
  const [img, setImg] = useState("");

  const uploadFile = async (file) => {
    if (!file) return;
    try {
      const res = await UploadApi.uploadImage(file);
      setImg(res.url);
    } catch {
      alert("Upload failed");
    }
  };

  useEffect(() => {
    if (!exam) return;
    setExamName(exam.examName);
    setExamType(exam.examType);
    setImg(exam.backgroundImageUrl || "");
  }, [exam]);

  const saveExam = () => {
    onUpdateExam({
      ...exam,
      examName,
      examType,
      backgroundImageUrl: img,
    });
  };

  // determine correct service
  const getService = (type) => {
    switch (type?.toLowerCase()) {
      case "reading": return readingService;
      case "listening": return listeningService;
      case "writing": return writingService;
      case "speaking": return speakingService;
      default: return null;
    }
  };

  // extract skillId based on type
  const getSkillId = (s) =>
    s.readingId || s.listeningId || s.writingId || s.speakingId || s.id;

  const getQuestionText = (s) =>
    s.readingQuestion ||
    s.listeningQuestion ||
    s.writingQuestion ||
    s.speakingQuestion ||
    "(no question text)";

  // load skills
  useEffect(() => {
    if (!show || !exam?.examId) return;

    const service = getService(exam.examType);
    if (!service?.getByExam) return;

    setLoading(true);
    service
      .getByExam(exam.examId)
      .then((data) => {
        const arr = Array.isArray(data) ? data : [];
        // sort by displayOrder so the list is in correct order
        const sorted = arr.sort((a, b) => (a.displayOrder ?? 9999) - (b.displayOrder ?? 9999));
        setSkills(sorted);
      })
      .catch(() => setSkills([]))
      .finally(() => setLoading(false));
  }, [show, exam]);

  // drag handlers
  const handleDragStart = (index) => {
    setDragIndex(index);
  };

  const handleDragOver = (e) => {
    e.preventDefault();
  };

  const handleDrop = async (index) => {
    if (dragIndex === null || dragIndex === index) return;

    const updated = [...skills];
    const moved = updated.splice(dragIndex, 1)[0];
    updated.splice(index, 0, moved);
    setSkills(updated);
    setDragIndex(null);

    await saveNewOrder(updated);
  };

  // this applies the new order to backend using existing update API
  const saveNewOrder = async (updatedList) => {
    const service = getService(exam.examType);
    if (!service?.update) return;

    for (let i = 0; i < updatedList.length; i++) {
      const item = updatedList[i];
      const skillId = getSkillId(item);

      try {
        await service.update(skillId, {
          displayOrder: i + 1,
        });
      } catch (err) {
        console.error("Failed to update order", err);
      }
    }
  };

  return (
    <PopupBase
      title={`Manage Exam — ${exam?.examName}`}
      icon={BookOpen}
      show={show}
      width="720px"
      onClose={onClose}
    >
      <div className={styles.popupScrollArea}>
        {/* Edit Exam Info */}
        <div className={styles.formCol} style={{ marginBottom: 20 }}>
          <label>Exam Name</label>
          <input
            type="text"
            value={examName}
            onChange={(e) => setExamName(e.target.value)}
          />

          <label>Exam Type</label>
          <select value={examType} onChange={(e) => setExamType(e.target.value)}>
            <option value="Reading">Reading</option>
            <option value="Listening">Listening</option>
            <option value="Writing">Writing</option>
            <option value="Speaking">Speaking</option>
          </select>

          <label>Background Image</label>

          {!img && (
            <div
              className={styles.imageBox}
              onClick={() => document.getElementById("edit-exam-image").click()}
            >
              <span className={styles.imageBoxText}>Click to upload</span>
            </div>
          )}

          <input
            id="edit-exam-image"
            type="file"
            accept="image/*"
            style={{ display: "none" }}
            onChange={(e) => uploadFile(e.target.files[0])}
          />

          {img && (
            <div className={styles.imagePreviewWrapper}>
              <img src={img} className={styles.imagePreview} />
              <button
                className={styles.imageRemoveBtn}
                onClick={() => setImg("")}
              >
                Remove
              </button>
            </div>
          )}

          <button className={styles.addBtn} onClick={saveExam}>
            Save Exam Info
          </button>
        </div>

        {/* Skills */}
        {loading ? (
          <p className={styles.loading}>
            <Loader2 size={18} className="spin" /> Loading...
          </p>
        ) : skills.length > 0 ? (
          <div className={styles.skillList}>
            {skills.map((s, i) => {
              const id = getSkillId(s);
              const text = getQuestionText(s);

              return (
                <div
                  key={id}
                  className={styles.skillItem}
                  draggable
                  onDragStart={() => handleDragStart(i)}
                  onDragOver={handleDragOver}
                  onDrop={() => handleDrop(i)}
                  style={{ cursor: "grab" }}
                >
                  <GripVertical size={18} style={{ marginRight: 8, opacity: 0.6 }} />

                  <div className={styles.skillText}>
                    <strong className={styles.skillId}>#{id}</strong>
                    <span>
                      {text.length > 120 ? text.slice(0, 120) + "…" : text}
                    </span>
                  </div>

                  <div className={styles.actions}>
                    <button onClick={() => onEdit(s)} title="Edit">
                      <Pencil size={16} />
                    </button>
                    <button onClick={() => onDelete(id)} title="Delete">
                      <Trash2 size={16} />
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        ) : (
          <p className={styles.empty}>No {examType} skills linked yet.</p>
        )}

        {/* Add Skill */}
        <div className={styles.footer}>
          <button className={styles.addBtn} onClick={onAddSkill}>
            <PlusCircle size={16} style={{ marginRight: 4 }} />
            Add Skill
          </button>
        </div>
      </div>
    </PopupBase>
  );
}
