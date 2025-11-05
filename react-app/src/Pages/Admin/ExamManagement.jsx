import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { formatTimeVietnam } from "../../utils/date";
import * as examService from "../../Services/ExamApi";
import * as UploadApi from "../../Services/UploadApi";
import * as readingService from "../../Services/ReadingApi";
import * as listeningService from "../../Services/ListeningApi";
import * as writingService from "../../Services/WritingApi";
import * as speakingService from "../../Services/SpeakingApi"; // 🆕 NEW
import ExamSkillModal from "../../Components/Admin/ExamPopup.jsx";
import Sidebar from "../../Components/Admin/AdminNavbar.jsx";
import styles from "./ExamManagement.module.css";

export default function ExamManagement() {
  const [examName, setExamName] = useState("");
  const [examType, setExamType] = useState("Reading");
  const [backgroundImageUrl, setBackgroundImageUrl] = useState("");
  const [uploading, setUploading] = useState(false);
  const [exams, setExams] = useState([]);
  const [skills, setSkills] = useState([]);
  const [selectedExam, setSelectedExam] = useState(null);
  const [status, setStatus] = useState("");
  const [showModal, setShowModal] = useState(false);
  const [filterType, setFilterType] = useState("All");
  const navigate = useNavigate();

  // ====== Service mapping ======
  const serviceMap = {
    Reading: readingService,
    Listening: listeningService,
    Writing: writingService,
    Speaking: speakingService, // 🆕 NEW
  };

  // ====== Load exams ======
  const fetchExams = async () => {
    try {
      const list = await examService.getAll();
      setExams(list);
    } catch (err) {
      console.error("❌ Failed to fetch exams:", err);
      setExams([]);
    }
  };

  // ====== Load skills for an exam ======
  const fetchSkills = async (examId, type) => {
    const service = serviceMap[type];
    if (!service) return console.error("⚠️ Unknown exam type:", type);

    try {
      const list = await service.getByExam(examId);
      setSkills(Array.isArray(list) ? list : []);
    } catch (err) {
      console.error(`❌ Failed to fetch ${type} skills:`, err);
      setSkills([]);
    }
  };

  useEffect(() => {
    fetchExams();
  }, []);

  // ====== Upload background image ======
  const handleImageUpload = async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    // Validate file type
    if (!file.type.startsWith('image/')) {
      setStatus("❌ Please select a valid image file");
      return;
    }

    // Validate file size (max 5MB)
    if (file.size > 5 * 1024 * 1024) {
      setStatus("❌ Image size must be less than 5MB");
      return;
    }

    setUploading(true);
    setStatus("Uploading image...");

    try {
      const result = await UploadApi.uploadImage(file);
      setBackgroundImageUrl(result.url);
      setStatus("✅ Image uploaded successfully");
    } catch (err) {
      console.error("Upload error:", err);
      setStatus("❌ Failed to upload image");
    } finally {
      setUploading(false);
    }
  };

  // ====== Create exam ======
  const handleSubmit = async (e) => {
    e.preventDefault();
    setStatus("Submitting...");

    try {
      const dataToSend = { 
        examName, 
        examType
      };
      
      // Chỉ thêm backgroundImageUrl nếu có giá trị
      if (backgroundImageUrl && backgroundImageUrl.trim() !== "") {
        dataToSend.backgroundImageUrl = backgroundImageUrl;
      }
      
      console.log("Sending exam data:", dataToSend); // Debug log
      
      const created = await examService.add(dataToSend);
      console.log("Created exam response:", created); // Debug log
      
      setStatus(`✅ Created exam "${created.examName}"`);
      setExamName("");
      setBackgroundImageUrl(""); // Reset after create
      fetchExams();
    } catch (err) {
      console.error("Error creating exam:", err);
      setStatus("❌ Failed to create exam");
    }
  };

  // ====== Manage exam ======
  const handleManageClick = (exam) => {
    setSelectedExam(exam);
    setShowModal(true);
    fetchSkills(exam.examId, exam.examType);
  };

  // ====== Determine path ======
  const getExamPath = (type) => {
    const map = {
      Reading: "add-reading",
      Listening: "add-listening",
      Writing: "add-writing",
      Speaking: "add-speaking", // 🆕 NEW PATH
    };
    return map[type] || "";
  };

  // ====== Add skill ======
  const handleAddSkill = () => {
    if (!selectedExam) return;
    const path = getExamPath(selectedExam.examType);
    if (!path) return alert("⚠️ Unknown exam type");
    navigate(path, { state: { exam: selectedExam } });
  };

  // ====== Edit skill ======
  const handleEditSkill = (skill) => {
    if (!selectedExam) return;
    const path = getExamPath(selectedExam.examType);
    if (!path) return alert("⚠️ Unknown exam type");
    navigate(path, { state: { exam: selectedExam, skill } });
  };

  // ====== Delete skill ======
  const handleDeleteSkill = async (skillId) => {
    if (!window.confirm("Are you sure you want to delete this question?"))
      return;

    const service = serviceMap[selectedExam.examType];
    if (!service) return;

    try {
      await service.remove(skillId);
      fetchSkills(selectedExam.examId, selectedExam.examType);
    } catch (err) {
      console.error("❌ Failed to delete skill:", err);
    }
  };

  // ====== Delete exam ======
  const handleDeleteExam = async (examId, examName) => {
    if (!window.confirm(`Are you sure you want to delete the exam "${examName}"? This action cannot be undone.`))
      return;

    try {
      await examService.remove(examId);
      setStatus(`✅ Deleted exam "${examName}"`);
      fetchExams();
    } catch (err) {
      console.error("❌ Failed to delete exam:", err);
      setStatus("❌ Failed to delete exam");
    }
  };

  // ====== Filter exams ======
  const filteredExams = filterType === "All" ? exams : exams.filter(exam => exam.examType === filterType);

  return (
    <>
      <Sidebar />
      <main className="admin-main">
        <div className={styles.dashboard}>
          {/* ===== Header ===== */}
          <header className={styles.header}>
            <h2>📚 Exam Management</h2>
            <button className={styles.exportBtn}>⬇️ Export CSV</button>
          </header>

          {/* ===== Create Exam ===== */}
          <section className={styles.card}>
            <form onSubmit={handleSubmit} className={styles.form}>
              <div className={styles.group}>
                <label>Exam Name</label>
                <input
                  type="text"
                  value={examName}
                  onChange={(e) => setExamName(e.target.value)}
                  placeholder="Enter exam name..."
                  required
                />
              </div>

              <div className={styles.group}>
                <label>Exam Type</label>
                <select
                  value={examType}
                  onChange={(e) => setExamType(e.target.value)}
                  required
                >
                  <option value="Reading">Reading</option>
                  <option value="Listening">Listening</option>
                  <option value="Writing">Writing</option>
                  <option value="Speaking">Speaking</option> {/* 🆕 ADDED */}
                </select>
              </div>

              <div className={styles.group}>
                <label>Background Image (Optional)</label>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  <input
                    id="background-image-upload"
                    type="file"
                    accept="image/*"
                    onChange={handleImageUpload}
                    disabled={uploading}
                    style={{ display: 'none' }}
                  />
                  <button 
                    type="button"
                    onClick={() => document.getElementById('background-image-upload')?.click()}
                    disabled={uploading}
                    style={{
                      padding: '10px 18px',
                      background: uploading ? '#ccc' : '#007bff',
                      color: 'white',
                      borderRadius: '8px',
                      cursor: uploading ? 'not-allowed' : 'pointer',
                      textAlign: 'center',
                      fontWeight: 600,
                      fontSize: '14px',
                      transition: 'background 0.2s ease',
                      border: 'none',
                      width: '100%'
                    }}
                    onMouseEnter={(e) => {
                      if (!uploading) e.target.style.background = '#0056b3';
                    }}
                    onMouseLeave={(e) => {
                      if (!uploading) e.target.style.background = '#007bff';
                    }}
                  >
                    {uploading ? '⏳ Uploading...' : '📷 Choose Image'}
                  </button>
                  {backgroundImageUrl && (
                    <div style={{ marginTop: '8px', display: 'flex', alignItems: 'flex-start', gap: '12px', flexWrap: 'wrap' }}>
                      <img 
                        src={backgroundImageUrl} 
                        alt="Preview" 
                        style={{ 
                          maxWidth: '200px', 
                          maxHeight: '150px', 
                          borderRadius: '8px',
                          border: '1px solid #e5e7eb',
                          objectFit: 'cover'
                        }} 
                      />
                      <button 
                        type="button"
                        onClick={() => setBackgroundImageUrl("")}
                        style={{ 
                          padding: '6px 14px',
                          background: '#ef4444',
                          color: 'white',
                          border: 'none',
                          borderRadius: '6px',
                          cursor: 'pointer',
                          fontSize: '14px',
                          fontWeight: 600,
                          transition: 'background 0.2s ease',
                          height: 'fit-content'
                        }}
                        onMouseEnter={(e) => e.target.style.background = '#dc3545'}
                        onMouseLeave={(e) => e.target.style.background = '#ef4444'}
                      >
                        ✕ Remove
                      </button>
                    </div>
                  )}
                </div>
              </div>

              <button type="submit" className={styles.btnPrimary} disabled={uploading}>
                + Create Exam
              </button>
            </form>
            {status && <p className={styles.status}>{status}</p>}
          </section>

          {/* ===== Exam List ===== */}
          <section className={styles.card}>
            <div className={styles.examListHeader}>
              <h3>Existing Exams</h3>
              <div className={styles.filterBar}>
                <label htmlFor="filterType" className={styles.filterLabel}>Filter by Type:</label>
                <select
                  id="filterType"
                  value={filterType}
                  onChange={(e) => setFilterType(e.target.value)}
                  className={styles.filterSelect}
                >
                  <option value="All">All Types</option>
                  <option value="Reading">Reading</option>
                  <option value="Listening">Listening</option>
                  <option value="Writing">Writing</option>
                  <option value="Speaking">Speaking</option>
                </select>
              </div>
            </div>
            <div className={styles.tableWrapper}>
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
                  {filteredExams.length ? (
                    filteredExams.map((exam) => (
                      <tr key={exam.examId}>
                        <td>{exam.examId}</td>
                        <td>{exam.examName}</td>
                        <td>{exam.examType}</td>
                        <td>
                          {exam.createdAt
                            ? formatTimeVietnam(exam.createdAt)
                            : ""}
                        </td>
                        <td>
                          <div style={{ display: 'flex', gap: '8px' }}>
                            <button
                              className={styles.btnManage}
                              onClick={() => handleManageClick(exam)}
                            >
                              Manage
                            </button>
                            <button
                              className={styles.btnDelete}
                              onClick={() => handleDeleteExam(exam.examId, exam.examName)}
                            >
                              Delete
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td
                        colSpan="5"
                        style={{ textAlign: "center", opacity: 0.6 }}
                      >
                        {filterType === "All" ? "No exams found." : `No ${filterType} exams found.`}
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </section>

          {/* ===== Modal ===== */}
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
      </main>
    </>
  );
}
