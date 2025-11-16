import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { formatTimeVietnam } from "../../utils/date";

import * as examService from "../../Services/ExamApi";
import * as readingService from "../../Services/ReadingApi";
import * as listeningService from "../../Services/ListeningApi";
import * as writingService from "../../Services/WritingApi";
import * as speakingService from "../../Services/SpeakingApi";

import Sidebar from "../../Components/Admin/AdminNavbar.jsx";
import ExamSkillModal from "../../Components/Admin/ExamPopup.jsx";
import CreateExamPopup from "../../Components/Admin/CreateExamPopup.jsx";
import AppLayout from "../../Components/Layout/AppLayout";

import { FileBadge, Search } from "lucide-react";
import styles from "./ExamManagement.module.css";

export default function ExamManagement() {
  const navigate = useNavigate();

  const [exams, setExams] = useState([]);
  const [skills, setSkills] = useState([]);
  const [search, setSearch] = useState("");

  const [selectedExam, setSelectedExam] = useState(null);
  const [showSkillModal, setShowSkillModal] = useState(false);
  const [showCreatePopup, setShowCreatePopup] = useState(false);

  const serviceMap = {
    Reading: readingService,
    Listening: listeningService,
    Writing: writingService,
    Speaking: speakingService,
  };

  useEffect(() => {
    loadExams();
  }, []);

  async function loadExams() {
    try {
      const res = await examService.getAll();
      setExams(res);
    } catch {
      setExams([]);
    }
  }

  const openSkillModal = (exam) => {
    setSelectedExam(exam);
    fetchSkills(exam.examId, exam.examType);
    setShowSkillModal(true);
  };

  const fetchSkills = async (id, type) => {
    try {
      const list = await serviceMap[type].getByExam(id);
      setSkills(list);
    } catch {
      setSkills([]);
    }
  };

  const filtered = exams.filter((e) =>
    e.examName.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <AppLayout sidebar={<Sidebar />} title="Exam Management">
      {/* NO CONTAINER — same as UserManagement */}
      <div className={styles.page}>

        {/* HEADER (BIG TEXT RESTORED) */}
        <div className="user-management-header">
          <div className="header-content">
            <FileBadge size={28} />
            <h1>Exam Management</h1>
            <span className="user-count">({filtered.length} exams)</span>
          </div>

          <button
            className="action-btn change-role"
            onClick={() => setShowCreatePopup(true)}
          >
            + Create Exam
          </button>
        </div>

        {/* SEARCH BAR */}
        <div className="search-section">
          <div className="search-container">
            <Search size={20} />
            <input
              type="text"
              placeholder="Search exam..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="search-input"
            />
          </div>
        </div>

        {/* TABLE */}
        <div className="users-table-container">
          <div className={styles.scrollWrapper}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Exam Name</th>
                  <th>Type</th>
                  <th>Created At</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {filtered.length ? (
                  filtered.map((exam) => (
                    <tr key={exam.examId}>
                      <td>#{exam.examId}</td>
                      <td>{exam.examName}</td>
                      <td>{exam.examType}</td>
                      <td>{formatTimeVietnam(exam.createdAt)}</td>
                      <td>
                        <button
                          className={styles.btnManage}
                          onClick={() => openSkillModal(exam)}
                        >
                          Manage
                        </button>
                        <button
                          className={styles.btnDelete}
                          onClick={() =>
                            examService.remove(exam.examId).then(loadExams)
                          }
                        >
                          Delete
                        </button>
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
        </div>

        {/* POPUPS */}
        <ExamSkillModal
          show={showSkillModal}
          exam={selectedExam}
          skills={skills}
          onClose={() => setShowSkillModal(false)}
          onEdit={(skill) =>
            navigate(`add-${selectedExam.examType.toLowerCase()}`, {
              state: { exam: selectedExam, skill },
            })
          }
          onDelete={(id) =>
            serviceMap[selectedExam.examType]
              .remove(id)
              .then(() => fetchSkills(selectedExam.examId, selectedExam.examType))
          }
          onAddSkill={() =>
            navigate(`add-${selectedExam.examType.toLowerCase()}`, {
              state: { exam: selectedExam },
            })
          }
        />

        <CreateExamPopup
          show={showCreatePopup}
          onClose={() => setShowCreatePopup(false)}
          onCreate={async (payload) => {
            await examService.add(payload);
            loadExams();
          }}
        />
      </div>
    </AppLayout>
  );
}
