import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as examService from "../../Services/ExamApi";
import * as writingService from "../../Services/WritingApi";
import ExamCard from "../../Components/Exam/ExamCard";
import ExamSkillModal from "../../Components/Exam/ExamPopup";
import NothingFound from "../../Components/Nothing/NothingFound";
import { Sparkles } from "lucide-react";
import styles from "./WritingPage.module.css";
import SearchBar from "../../Components/Common/SearchBar"; // ✅ import SearchBar
import sadCloud from "../../assets/sad_cloud.png";
export default function WritingPage() {
  const navigate = useNavigate();

  const [exams, setExams] = useState([]);
  const [filteredExams, setFilteredExams] = useState([]); // ✅ for search results
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [activeExam, setActiveExam] = useState(null);
  const [examTasks, setExamTasks] = useState([]);
  const [loadingDetail, setLoadingDetail] = useState(false);

  // ====== Fetch all exams ======
  useEffect(() => {
    let mounted = true;
    examService
      .getAll()
      .then((data) => {
        if (!mounted) return;
        const list = Array.isArray(data)
          ? data.filter((e) => e.examType === "Writing")
          : [];
        setExams(list);
        setFilteredExams(list); // ✅ initialize
      })
      .catch((err) => {
        console.error(err);
        if (mounted) setError("Failed to load Writing exams.");
      })
      .finally(() => mounted && setLoading(false));
    return () => (mounted = false);
  }, []);

  // ====== Handle search (debounce is handled inside SearchBar) ======
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

  // ====== Load tasks for selected exam ======
  const handleTakeExam = (exam) => {
    setActiveExam(exam);
    setLoadingDetail(true);
    setExamTasks([]);

    writingService
      .getByExam(exam.examId)
      .then((data) => {
        const list = Array.isArray(data) ? data : [];
        setExamTasks(list);
      })
      .catch((err) => {
        console.error(err);
        alert("Failed to load writing tasks for this exam.");
      })
      .finally(() => setLoadingDetail(false));
  };

  // ====== Callbacks from Modal ======
  const handleStartFullTest = (exam, duration) => {
    navigate("/writing/test", {
      state: {
        exam,
        tasks: examTasks,
        mode: "full",
        duration,
      },
    });
  };

  const handleStartIndividual = (exam, task) => {
    const duration = task.writingType === "Task 1" ? 20 : 40;
    navigate("/writing/test", {
      state: {
        exam,
        task,
        tasks: [task],
        mode: "single",
        duration,
      },
    });
  };

  // ====== Render ======
  return (
    <AppLayout title="Writing" sidebar={<GeneralSidebar />}>
      <div className={styles.container}>
        <h2 className={styles.pageTitle}>IELTS Writing</h2>

        {loading && <div className={styles.stateText}>Loading…</div>}
        {!loading && error && <div className={styles.errorText}>{error}</div>}

        {!loading && !error && (
          <>
            {/* ✅ Search bar added here */}
            <div style={{ marginBottom: "20px" }}>
              <SearchBar onSearch={handleSearch} />
            </div>

            <div className={styles.grid}>
              {filteredExams.length > 0 ? (
                filteredExams.map((exam) => (
                  <div key={exam.examId} className={styles.examCardWrapper}>
                    <ExamCard exam={exam} onTake={() => handleTakeExam(exam)} />
                    <div className={styles.examFeatures}>
                      <span className={styles.featureTag}>
                        <Sparkles size={14} />
                        AI-Powered
                      </span>
                    </div>
                  </div>
                ))
              ) : (
                <div className={styles.centerWrapper}>
                  <NothingFound
                    imageSrc= {sadCloud}
                    title="No writing exams found"
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
          tasks={examTasks}
          loading={loadingDetail}
          onClose={() => setActiveExam(null)}
          onStartFullTest={handleStartFullTest}
          onStartIndividual={handleStartIndividual}
        />
      )}
    </AppLayout>
  );
}
