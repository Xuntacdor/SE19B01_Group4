import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as examService from "../../Services/ExamApi";
import * as listeningService from "../../Services/ListeningApi";
import ExamCard from "../../Components/Exam/ExamCard";
import ExamSkillModal from "../../Components/Exam/ExamPopup";
import styles from "./ListeningPage.module.css";
import NothingFound from "../../Components/Nothing/NothingFound";
import SearchBar from "../../Components/Common/SearchBar"; // ✅ import SearchBar

export default function ListeningPage() {
  const navigate = useNavigate();

  const [exams, setExams] = useState([]);
  const [filteredExams, setFilteredExams] = useState([]); // ✅ for search
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [activeExam, setActiveExam] = useState(null);
  const [examQuestions, setExamQuestions] = useState([]);
  const [loadingDetail, setLoadingDetail] = useState(false);

  // ====== Fetch all exams and filter Listening ======
  useEffect(() => {
    let mounted = true;
    examService
      .getAll()
      .then((data) => {
        if (!mounted) return;
        const list = Array.isArray(data)
          ? data.filter((e) => e.examType === "Listening")
          : [];
        setExams(list);
        setFilteredExams(list); // ✅ initialize filtered list
      })
      .catch((err) => {
        console.error(err);
        if (mounted) setError("Failed to load Listening exams.");
      })
      .finally(() => mounted && setLoading(false));
    return () => (mounted = false);
  }, []);

  // ====== Handle search (debounce handled inside SearchBar) ======
  const handleSearch = (query) => {
    if (!query.trim()) {
      setFilteredExams(exams);
      return;
    }
    const lower = query.toLowerCase();
    const result = exams.filter(
      (exam) =>
        exam.examName.toLowerCase().includes(lower) ||
        exam.description?.toLowerCase().includes(lower)
    );
    setFilteredExams(result);
  };

  // ====== Load questions for selected exam ======
  const handleTakeExam = (exam) => {
    setActiveExam(exam);
    setLoadingDetail(true);
    setExamQuestions([]);

    listeningService
      .getByExam(exam.examId)
      .then((data) => {
        const list = Array.isArray(data) ? data : [];
        setExamQuestions(list);
      })
      .catch((err) => {
        console.error(err);
        alert("Failed to load listening questions for this exam.");
      })
      .finally(() => setLoadingDetail(false));
  };

  // ====== Callbacks for modal ======
  const handleStartFullTest = (exam, duration) => {
    if (loadingDetail || examQuestions.length === 0) return;
    navigate("/listening/test", {
      state: {
        exam,
        tasks: examQuestions,
        mode: "full",
        duration,
      },
    });
  };

  const handleStartIndividual = (exam, task) => {
    if (loadingDetail || !task) return;
    navigate("/listening/test", {
      state: {
        exam,
        tasks: [task],
        mode: "single",
        duration: 20,
      },
    });
  };

  // ====== Render ======
  return (
    <AppLayout title="Listening Page" sidebar={<GeneralSidebar />}>
      <div className={styles.container}>
        <h2 className={styles.pageTitle}>IELTS Listening</h2>

        {loading && <div className={styles.stateText}>Loading…</div>}
        {!loading && error && <div className={styles.errorText}>{error}</div>}

        {!loading && !error && (
          <>
            {/* ✅ Search bar */}
            <div style={{ marginBottom: "20px" }}>
              <SearchBar onSearch={handleSearch} />
            </div>

            <div className={styles.grid}>
              {filteredExams.length > 0 ? (
                filteredExams.map((exam) => (
                  <ExamCard
                    key={exam.examId}
                    exam={exam}
                    onTake={() => handleTakeExam(exam)}
                  />
                ))
              ) : (
                <div className={styles.centerWrapper}>
                  <NothingFound
                    imageSrc="/src/assets/sad_cloud.png"
                    title="No listening exams found"
                    message="Try adjusting your search keywords."
                  />
                </div>
              )}
            </div>
          </>
        )}
      </div>

      {activeExam && (
        <>
          {/* ✅ Loading overlay for exam data */}
          {loadingDetail && (
            <div className={styles.loadingOverlay}>
              <div className={styles.loadingBox}>
                <div className={styles.spinner}></div>
                <p>Please wait, loading exam data…</p>
              </div>
            </div>
          )}

          <ExamSkillModal
            exam={activeExam}
            tasks={examQuestions}
            loading={loadingDetail}
            onClose={() => setActiveExam(null)}
            onStartFullTest={handleStartFullTest}
            onStartIndividual={handleStartIndividual}
          />
        </>
      )}
    </AppLayout>
  );
}
