import React from "react";
import { useLocation, useNavigate } from "react-router-dom";
import ExamSkillPage from "../../Components/Exam/ExamSkillPage";
import styles from "./ReadingExamPage.module.css";

export default function ReadingExamPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const { exam, tasks, duration } = state || {};

  return (
    <ExamSkillPage
      exam={exam}
      tasks={tasks}
      duration={duration}
      skillKey="readingId"
      skillType="Reading"
      renderContent={(task) => (
        <div
          className={styles.readingContent}
          dangerouslySetInnerHTML={{ __html: task.readingContent || "" }}
        />
      )}
      renderQuestion={(task) => (
        <div
          className={styles.question}
          dangerouslySetInnerHTML={{
            __html: task.questionHtml || task.readingQuestion,
          }}
        />
      )}
      onBack={() => navigate("/reading")}
    />
  );
}
