import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as examService from "../../Services/ExamApi";
import * as readingService from "../../Services/ReadingApi";
import ExamCard from "../../Components/Exam/ExamCard";
import ExamSkillModal from "../../Components/Exam/ExamPopup";
import styles from "./ReadingPage.module.css";
import NothingFound from "../../Components/Nothing/NothingFound";
import SearchBar from "../../Components/Common/SearchBar"; // ✅ import SearchBar

export default function ReadingPage() {
  const navigate = useNavigate();

  const [exams, setExams] = useState([]);
  const [filteredExams, setFilteredExams] = useState([]); // ✅ dùng để lọc
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [activeExam, setActiveExam] = useState(null);
  const [examQuestions, setExamQuestions] = useState([]);
  const [loadingDetail, setLoadingDetail] = useState(false);

  // ====== Fetch all exams and filter Reading ======
  useEffect(() => {
    let mounted = true;
    examService
      .getAll()
      .then((data) => {
        if (!mounted) return;
        console.log("All exams fetched:", data);
        const list = Array.isArray(data)
          ? data.filter((e) => e.examType === "Reading")
          : [];
        console.log("Filtered Reading exams:", list);
        setExams(list);
        setFilteredExams(list); // ✅ khởi tạo filtered list
      })
      .catch((err) => {
        console.error("Error fetching exams:", err);
        if (mounted) setError("Failed to load Reading exams.");
      })
      .finally(() => mounted && setLoading(false));
    return () => (mounted = false);
  }, []);

  // ====== Handle search (debounce trong SearchBar) ======
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

    readingService
      .getByExam(exam.examId)
      .then((data) => {
        console.log(
          "Reading questions fetched for exam",
          exam.examId,
          ":",
          data
        );
        const list = Array.isArray(data) ? data : [];
        setExamQuestions(list);
      })
      .catch((err) => {
        console.error("Error fetching reading questions:", err);
        alert("Failed to load reading questions for this exam.");
      })
      .finally(() => setLoadingDetail(false));
  };

  // ====== Callbacks from Modal ======
  const handleStartFullTest = (exam, duration) => {
    navigate("/reading/test", {
      state: {
        exam,
        tasks: examQuestions,
        mode: "full",
        duration,
      },
    });
  };

  const handleStartIndividual = (exam, task) => {
    navigate("/reading/test", {
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
    <AppLayout title="Reading" sidebar={<GeneralSidebar />}>
      <div className={styles.container}>
        <h2 className={styles.pageTitle}>IELTS Reading</h2>

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
                    title="No reading exams found"
                    message="Try adjusting your search keywords."
                  />
                </div>
              )}
            </div>
          </>
        )}
      </div>

      {activeExam && (
        <ExamSkillModal
          exam={activeExam}
          tasks={examQuestions}
          loading={loadingDetail}
          onClose={() => setActiveExam(null)}
          onStartFullTest={handleStartFullTest}
          onStartIndividual={handleStartIndividual}
        />
      )}
    </AppLayout>
  );
}
