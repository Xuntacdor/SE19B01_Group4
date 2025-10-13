import React from "react";
import { useLocation, useNavigate } from "react-router-dom";
import ExamSkillPage from "../../Components/Exam/ExamSkillPage";
import styles from "./ListeningExamPage.module.css";

export default function ListeningExamPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const { exam, tasks, duration } = state || {};

  return (
    <ExamSkillPage
      exam={exam}
      tasks={tasks}
      duration={duration}
      skillKey="listeningId"
      skillType="Listening"
      renderContent={(task) => (
        <div className={styles.audioSection}>
          {task.audioUrl ? (
            <audio controls className={styles.audioPlayer}>
              <source src={task.audioUrl} type="audio/mpeg" />
              Your browser does not support the audio element.
            </audio>
          ) : (
            <p>No audio available.</p>
          )}
        </div>
      )}
      renderQuestion={(task) => (
        <div
          className={styles.question}
          dangerouslySetInnerHTML={{
            __html: task.questionHtml || task.listeningQuestion,
          }}
        />
      )}
      onBack={() => navigate("/listening")}
    />
  );
}
