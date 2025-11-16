import PopupBase from "../Common/PopupBase";
import React, { useState, useEffect } from "react";
import styles from "./ExamPopup.module.css";

import * as readingService from "../../Services/ReadingApi";
import * as listeningService from "../../Services/ListeningApi";
import * as writingService from "../../Services/WritingApi";
import * as speakingService from "../../Services/SpeakingApi";
import * as UploadApi from "../../Services/UploadApi";

import {
  Pencil,
  Trash2,
  PlusCircle,
  Loader2,
  BookOpen,
  GripVertical
} from "lucide-react";

import DeleteConfirmPopup from "../Common/DeleteConfirmPopup";

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

  // EDIT EXAM INFO
  const [examName, setExamName] = useState("");
  const [examType, setExamType] = useState("");
  const [img, setImg] = useState("");

  // DELETE POPUP FOR SKILL
  const [showSkillDeletePopup, setShowSkillDeletePopup] = useState(false);
  const [skillToDelete, setSkillToDelete] = useState(null);

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

  // SERVICE SELECTOR
  const getService = (type) => {
    switch (type?.toLowerCase()) {
      case "reading":
        return readingService;
      case "listening":
        return listeningService;
      case "writing":
        return writingService;
      case "speaking":
        return speakingService;
      default:
        return null;
    }
  };

  const getSkillId = (s) =>
    s.readingId ||
    s.listeningId ||
    s.writingId ||
    s.speakingId ||
    s.id;

  const getQuestionText = (s) =>
    s.readingQuestion ||
    s.listeningQuestion ||
    s.writingQuestion ||
    s.speakingQuestion ||
    "(no question text)";

  // LOAD SKILLS
  useEffect(() => {
    if (!show || !exam?.examId) return;

    const service = getService(exam.examType);
    if (!service?.getByExam) return;

    setLoading(true);

    service
      .getByExam(exam.examId)
      .then((data) => {
        const arr = Array.isArray(data) ? data : [];
        const sorted = arr.sort(
          (a, b) => (a.displayOrder ?? 9999) - (b.displayOrder ?? 9999)
        );
        setSkills(sorted);
      })
      .catch(() => setSkills([]))
      .finally(() => setLoading(false));
  }, [show, exam]);

  // DRAG & DROP ORDER
  const handleDragStart = (i) => setDragIndex(i);

  const handleDragOver = (e) => e.preventDefault();

  const handleDrop = async (i) => {
    if (dragIndex === null || dragIndex === i) return;

    const updated = [...skills];
    const moved = updated.splice(dragIndex, 1)[0];
    updated.splice(i, 0, moved);

    setSkills(updated);
    setDragIndex(null);

    await saveNewOrder(updated);
  };

  const saveNewOrder = async (list) => {
    const service = getService(exam.examType);
    if (!service?.update) return;

    for (let i = 0; i < list.length; i++) {
      const skill = list[i];
      const skillId = getSkillId(skill);

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
    <>
      <PopupBase
        title={`Manage Exam — ${exam?.examName}`}
        icon={BookOpen}
        show={show}
        width="720px"
        onClose={onClose}
      >
        <div className={styles.popupScrollArea}>
          {/* EDIT EXAM INFO */}
          <div className={styles.formCol} style={{ marginBottom: 20 }}>
            <label>Exam Name</label>
            <input
              type="text"
              value={examName}
              onChange={(e) => setExamName(e.target.value)}
            />

            <label>Exam Type</label>
            <select
              value={examType}
              onChange={(e) => setExamType(e.target.value)}
            >
              <option value="Reading">Reading</option>
              <option value="Listening">Listening</option>
              <option value="Writing">Writing</option>
              <option value="Speaking">Speaking</option>
            </select>

            <label>Background Image</label>

            {!img && (
              <div
                className={styles.imageBox}
                onClick={() =>
                  document.getElementById("edit-exam-image").click()
                }
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

          {/* SKILL LIST */}
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
                    <GripVertical
                      size={18}
                      style={{ marginRight: 8, opacity: 0.6 }}
                    />

                    <div className={styles.skillText}>
                      <strong className={styles.skillId}>#{id}</strong>
                      <span>
                        {text.length > 120
                          ? text.slice(0, 120) + "…"
                          : text}
                      </span>
                    </div>

                    <div className={styles.actions}>
                      <button onClick={() => onEdit(s)} title="Edit">
                        <Pencil size={16} />
                      </button>

                      <button
                        onClick={() => {
                          setSkillToDelete(id);
                          setShowSkillDeletePopup(true);
                        }}
                        title="Delete"
                      >
                        <Trash2 size={16} />
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          ) : (
            <p className={styles.empty}>
              No {examType} skills linked yet.
            </p>
          )}

          {/* ADD SKILL BUTTON */}
          <div className={styles.footer}>
            <button className={styles.addBtn} onClick={onAddSkill}>
              <PlusCircle size={16} style={{ marginRight: 4 }} />
              Add Skill
            </button>
          </div>
        </div>
      </PopupBase>

      {/* DELETE CONFIRM POPUP */}
      <DeleteConfirmPopup
        show={showSkillDeletePopup}
        title="Delete Skill"
        message="Are you sure you want to delete this skill?"
        onCancel={() => setShowSkillDeletePopup(false)}
        onConfirm={async () => {
          await onDelete(skillToDelete);
          setShowSkillDeletePopup(false);
        }}
      />
    </>
  );
}
