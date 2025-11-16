import React, { useState } from "react";
import PopupBase from "../Common/PopupBase";
import styles from "./ExamPopup.module.css";
import * as UploadApi from "../../Services/UploadApi";

export default function CreateExamPopup({ show, onClose, onCreate }) {
  const [examName, setExamName] = useState("");
  const [examType, setExamType] = useState("Reading");
  const [img, setImg] = useState("");
  const [uploading, setUploading] = useState(false);

  const uploadFile = async (file) => {
    if (!file) return;
    setUploading(true);
    try {
      const res = await UploadApi.uploadImage(file);
      setImg(res.url);
    } catch {
      alert("Upload failed");
    }
    setUploading(false);
  };

  const handleImageClick = () => {
    document.getElementById("exam-image-input").click();
  };

  const submit = () => {
    onCreate({
      examName,
      examType,
      backgroundImageUrl: img || null,
    });
    onClose();
  };

  return (
    <PopupBase
      title="Create New Exam"
      show={show}
      width="620px"
      onClose={onClose}
    >
      <div className={styles.popupScrollArea}>
        <div className={styles.formCol}>
          <label>Exam Name</label>
          <input
            type="text"
            value={examName}
            onChange={(e) => setExamName(e.target.value)}
            required
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
            <div className={styles.imageBox} onClick={handleImageClick}>
              <span className={styles.imageBoxText}>
                {uploading ? "Uploading..." : "Click to upload image"}
              </span>
            </div>
          )}

          <input
            id="exam-image-input"
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

          <button className={styles.addBtn} onClick={submit}>
            Create Exam
          </button>
        </div>
      </div>
    </PopupBase>
  );
}
