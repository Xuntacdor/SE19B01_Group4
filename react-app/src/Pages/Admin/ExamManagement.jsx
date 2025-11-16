import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { formatTimeVietnam } from "../../utils/date";

// Services
import * as examService from "../../Services/ExamApi";
import * as UploadApi from "../../Services/UploadApi";
import * as readingService from "../../Services/ReadingApi";
import * as listeningService from "../../Services/ListeningApi";
import * as writingService from "../../Services/WritingApi";
import * as speakingService from "../../Services/SpeakingApi";

// Components
import Sidebar from "../../Components/Admin/AdminNavbar.jsx";
import ExamSkillModal from "../../Components/Admin/ExamPopup.jsx";
import AppLayout from "../../Components/Layout/AppLayout";

// Styles
import styles from "./ExamManagement.module.css";

export default function ExamManagement() {
  const navigate = useNavigate();

  const [examName, setExamName] = useState("");
  const [examType, setExamType] = useState("Reading");
  const [backgroundImageUrl, setBackgroundImageUrl] = useState("");
  const [uploading, setUploading] = useState(false);
  const [status, setStatus] = useState("");
  const [search, setSearch] = useState("");

  const [exams, setExams] = useState([]);
  const [skills, setSkills] = useState([]);
  const [filterType, setFilterType] = useState("All");

  const [selectedExam, setSelectedExam] = useState(null);
  const [showModal, setShowModal] = useState(false);

  const serviceMap = {
    Reading: readingService,
    Listening: listeningService,
    Writing: writingService,
    Speaking: speakingService,
  };

  const fetchExams = async () => {
    try {
      const list = await examService.getAll();
      setExams(list);
    } catch {
      setExams([]);
    }
  };

  useEffect(() => {
    fetchExams();
  }, []);

  const fetchSkills = async (examId, type) => {
    try {
      const list = await serviceMap[type].getByExam(examId);
      setSkills(Array.isArray(list) ? list : []);
    } catch {
      setSkills([]);
    }
  };

  const handleImageUpload = async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    if (!file.type.startsWith("image/")) {
      setStatus("❌ File must be an image");
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      setStatus("❌ File too large (max 5MB)");
      return;
    }

    setUploading(true);
    setStatus("Uploading...");

    try {
      const res = await UploadApi.uploadImage(file);
      setBackgroundImageUrl(res.url);
      setStatus("✅ Uploaded");
    } catch {
      setStatus("❌ Upload failed");
    }

    setUploading(false);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    try {
      const payload = { examName, examType };
      if (backgroundImageUrl.trim() !== "")
        payload.backgroundImageUrl = backgroundImageUrl;

      await examService.add(payload);

      setStatus(`Created exam "${examName}"`);
      setExamName("");
      setBackgroundImageUrl("");
      fetchExams();
    } catch {
      setStatus("❌ Failed");
    }
  };

  const handleManageClick = (exam) => {
    setSelectedExam(exam);
    setShowModal(true);
    fetchSkills(exam.examId, exam.examType);
  };

  const getPath = (type) =>
    ({
      Reading: "add-reading",
      Listening: "add-listening",
      Writing: "add-writing",
      Speaking: "add-speaking",
    }[type]);

  const handleAddSkill = () =>
    navigate(getPath(selectedExam.examType), { state: { exam: selectedExam } });

  const handleEditSkill = (skill) =>
    navigate(getPath(selectedExam.examType), {
      state: { exam: selectedExam, skill },
    });

  const handleDeleteSkill = async (skillId) => {
    if (!window.confirm("Delete this question?")) return;
    await serviceMap[selectedExam.examType].remove(skillId);
    fetchSkills(selectedExam.examId, selectedExam.examType);
  };

  const handleDeleteExam = async (id, name) => {
    if (!window.confirm(`Delete "${name}"?`)) return;
    await examService.remove(id);
    setStatus(`Deleted "${name}"`);
    fetchExams();
  };

  // search + filter
  const filteredExams =
    filterType === "All"
      ? exams
      : exams.filter((e) => e.examType === filterType);

  const searchedExams = filteredExams.filter((e) =>
    e.examName.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <AppLayout sidebar={<Sidebar />} title="Exam Management">
      <div className={styles.page}>
        <div className={styles.container}>
          {/* ===========================
          CREATE EXAM SECTION
      ============================ */}
          <section className={styles.createSection}>
            <h3 className={styles.sectionTitle}>Create Exam</h3>

            <form onSubmit={handleSubmit} className={styles.formRow}>
              {/* Exam Name */}
              <div className={styles.inputGroup}>
                <label>Exam Name</label>
                <input
                  type="text"
                  value={examName}
                  onChange={(e) => setExamName(e.target.value)}
                  placeholder="Enter exam name..."
                  required
                />
              </div>

              {/* Exam Type */}
              <div className={styles.inputGroup}>
                <label>Exam Type</label>
                <select
                  value={examType}
                  onChange={(e) => setExamType(e.target.value)}
                  required
                >
                  <option value="Reading">Reading</option>
                  <option value="Listening">Listening</option>
                  <option value="Writing">Writing</option>
                  <option value="Speaking">Speaking</option>
                </select>
              </div>

              {/* Upload Image */}
              <div className={styles.inputGroup}>
                <label>Background Image</label>

                <button
                  type="button"
                  className={styles.imageUploadBtn}
                  disabled={uploading}
                  onClick={() =>
                    document.getElementById("background-image-upload").click()
                  }
                >
                  {uploading ? "⏳ Uploading..." : "📷 Choose Image"}
                </button>

                <input
                  id="background-image-upload"
                  type="file"
                  accept="image/*"
                  onChange={handleImageUpload}
                  disabled={uploading}
                  style={{ display: "none" }}
                />
              </div>

              {/* Create Button */}
              <button
                type="submit"
                className={styles.submitBtn}
                disabled={uploading}
              >
                + Create
              </button>
            </form>

            {/* Image Preview - below form */}
            {backgroundImageUrl && (
              <div className={styles.imagePreviewWrapper}>
                <img
                  src={backgroundImageUrl}
                  alt="Preview"
                  className={styles.imagePreview}
                />
                <button
                  className={styles.imageRemoveBtn}
                  onClick={() => setBackgroundImageUrl("")}
                >
                  ✕ Remove
                </button>
              </div>
            )}

            {status && <p className={styles.status}>{status}</p>}
          </section>

          {/* ===========================
          EXAM LIST SECTION
      ============================ */}
          <section className={styles.examListSection}>
            {/* Title + Controls */}
            <div className={styles.listHeader}>
              <h3 className={styles.sectionTitle}>Existing Exams</h3>

              <div className={styles.controlsRow}>
                {/* Search */}
                <input
                  className={styles.searchBar}
                  type="text"
                  placeholder="Search exam..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />

                {/* Filter */}
                <select
                  className={styles.filterSelect}
                  value={filterType}
                  onChange={(e) => setFilterType(e.target.value)}
                >
                  <option value="All">All Types</option>
                  <option value="Reading">Reading</option>
                  <option value="Listening">Listening</option>
                  <option value="Writing">Writing</option>
                  <option value="Speaking">Speaking</option>
                </select>
              </div>
            </div>

            {/* Scrollable Table */}
            <div className={styles.scrollWrapper}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Name</th>
                    <th>Type</th>
                    <th>Created At</th>
                    <th>Action</th>
                  </tr>
                </thead>

                <tbody>
                  {searchedExams.length ? (
                    searchedExams.map((exam) => (
                      <tr key={exam.examId}>
                        <td>{exam.examId}</td>
                        <td>{exam.examName}</td>
                        <td>{exam.examType}</td>
                        <td>{formatTimeVietnam(exam.createdAt)}</td>
                        <td>
                          <div style={{ display: "flex", gap: "8px" }}>
                            <button
                              className={styles.btnManage}
                              onClick={() => handleManageClick(exam)}
                            >
                              Manage
                            </button>

                            <button
                              className={styles.btnDelete}
                              onClick={() =>
                                handleDeleteExam(exam.examId, exam.examName)
                              }
                            >
                              Delete
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan="5" className={styles.empty}>
                        No exams found.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </section>

          <ExamSkillModal
            show={showModal}
            exam={selectedExam}
            skills={skills}
            onClose={() => setShowModal(false)}
            onEdit={handleEditSkill}
            onDelete={handleDeleteSkill}
            onAddSkill={handleAddSkill}
          />
        </div>
      </div>
    </AppLayout>
  );
}
