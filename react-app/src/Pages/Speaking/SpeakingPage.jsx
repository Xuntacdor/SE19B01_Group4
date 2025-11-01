import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import * as examService from "../../Services/ExamApi";
import * as speakingService from "../../Services/SpeakingApi";
import ExamCard from "../../Components/Exam/ExamCard";
import ExamSkillModal from "../../Components/Exam/ExamPopup";
import NothingFound from "../../Components/Nothing/NothingFound";
import { Mic, Headphones, Clock, Users } from "lucide-react";
import styles from "./SpeakingPage.module.css";

export default function SpeakingPage() {
  const navigate = useNavigate();

  const [exams, setExams] = useState([]);
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
          ? data.filter((e) => e.examType === "Speaking")
          : [];
        setExams(list);
      })
      .catch((err) => {
        console.error(err);
        if (mounted) setError("Failed to load Speaking exams.");
      })
      .finally(() => mounted && setLoading(false));
    return () => (mounted = false);
  }, []);

  // ====== Load tasks for selected exam ======
  const handleTakeExam = (exam) => {
    setActiveExam(exam);
    setLoadingDetail(true);
    setExamTasks([]);

    speakingService
      .getByExam(exam.examId)
      .then((data) => {
        console.log("=== SPEAKING DATA ===", data);
        const list = Array.isArray(data) ? data : [];
        setExamTasks(list);
      })
      .catch((err) => {
        console.error(err);
        alert("Failed to load speaking tasks for this exam.");
      })
      .finally(() => setLoadingDetail(false));
  };

  // ====== Callbacks from Modal ======
  const handleStartFullTest = (exam, duration) => {
    navigate("/speaking/test", {
      state: {
        exam,
        tasks: examTasks,
        mode: "full",
        duration,
      },
    });
  };

  const handleStartIndividual = (exam, task, duration, selectedTasks) => {
    const tasksToSend = selectedTasks?.length ? selectedTasks : [task];

    navigate("/speaking/test", {
      state: {
        exam,
        task: tasksToSend[0],
        tasks: tasksToSend,
        mode: selectedTasks?.length > 1 ? "part" : "single",
        duration,
      },
    });
  };

  const getTaskDuration = (speakingType) => {
    switch (speakingType) {
      case "Part 1":
        return 4; // 4-5 minutes
      case "Part 2":
        return 3; // 3-4 minutes
      case "Part 3":
        return 4; // 4-5 minutes
      default:
        return 5;
    }
  };

  // ====== Render ======
  return (
    <AppLayout title="Speaking" sidebar={<GeneralSidebar />}>
      <div className={styles.container}>
        {loading && <div className={styles.stateText}>Loading…</div>}
        {!loading && error && <div className={styles.errorText}>{error}</div>}

        {!loading && !error && (
          <div className={styles.examsSection}>
            <h3 className={styles.sectionTitle}>Speaking Tests</h3>
            <div className={styles.grid}>
              {exams.length > 0 ? (
                exams.map((exam) => (
                  <div key={exam.examId} className={styles.examCardWrapper}>
                    <ExamCard exam={exam} onTake={() => handleTakeExam(exam)} />
                    <div className={styles.examFeatures}>
                      <span className={styles.featureTag}>
                        <Mic size={14} />
                        AI-Powered
                      </span>
                      <span className={styles.featureTag}>
                        <Clock size={14} />
                        Real-time
                      </span>
                    </div>
                  </div>
                ))
              ) : (
                <div className={styles.centerWrapper}>
                  <NothingFound
                    imageSrc="/src/assets/sad_cloud.png"
                    title="No speaking exams available"
                    message="Please check back later or try another skill."
                  />
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {activeExam && (
        <ExamSkillModal
          exam={activeExam}
          tasks={examTasks}
          loading={loadingDetail}
          onStartFullTest={handleStartFullTest}
          onStartIndividual={handleStartIndividual}
          onClose={() => setActiveExam(null)}
          skillType="Speaking"
        />
      )}
    </AppLayout>
  );
}
