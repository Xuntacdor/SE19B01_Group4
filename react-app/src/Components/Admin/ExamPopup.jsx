import PopupBase from "../Common/PopupBase";
import React, { useState, useEffect } from "react";
import styles from "./ExamPopup.module.css";
import * as readingService from "../../Services/ReadingApi";
import * as listeningService from "../../Services/ListeningApi";
import * as writingService from "../../Services/WritingApi";
import * as speakingService from "../../Services/SpeakingApi";
import { Pencil, Trash2, PlusCircle, Loader2, BookOpen } from "lucide-react";

export default function ExamManageSkill({
  show,
  exam,
  onClose,
  onEdit,
  onDelete,
  onAddSkill,
}) {
  const [skills, setSkills] = useState([]);
  const [loading, setLoading] = useState(true);

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

  useEffect(() => {
    if (!show || !exam?.examId) return;
    const service = getService(exam.examType);
    if (!service?.getByExam) return;

    setLoading(true);
    service
      .getByExam(exam.examId)
      .then((data) => setSkills(Array.isArray(data) ? data : []))
      .catch(() => setSkills([]))
      .finally(() => setLoading(false));
  }, [show, exam]);

  const examName = exam?.examName ?? "";
  const examType = exam?.examType ?? "";

  const getSkillId = (s) =>
    s.readingId || s.listeningId || s.writingId || s.speakingId || s.id;
  const getQuestionText = (s) =>
    s.readingQuestion ||
    s.listeningQuestion ||
    s.writingQuestion ||
    s.speakingQuestion ||
    "(no question text)";

  return (
    <PopupBase
      title={`Manage ${examType} Skills (${examName})`}
      icon={BookOpen}
      show={show}
      width="700px"
      onClose={onClose}
    >
      {loading ? (
        <p className={styles.loading}>
          <Loader2 size={18} className="spin" /> Loading...
        </p>
      ) : skills.length > 0 ? (
        <div className={styles.skillList}>
          {skills.map((s, i) => {
            const id = getSkillId(s) ?? i;
            const text = getQuestionText(s);
            return (
              <div key={id} className={styles.skillItem}>
                <div className={styles.skillText}>
                  <strong className={styles.skillId}>#{id}</strong>{" "}
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

      <div className={styles.footer}>
        <button className={styles.addBtn} onClick={onAddSkill}>
          <PlusCircle size={16} style={{ marginRight: 4 }} /> Add Skill
        </button>
      </div>
    </PopupBase>
  );
}
