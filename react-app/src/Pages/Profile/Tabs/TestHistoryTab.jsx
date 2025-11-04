import React, { useState } from "react";
import { CalendarDays, ClipboardList } from "lucide-react";
import NothingFound from "../../../Components/Nothing/NothingFound";
import PopupBase from "../../../Components/Common/PopupBase";
import { formatFullDateVietnam, formatRelativeTime } from "../../../utils/date";
import "./TestHistoryTab.css";

export default function TestHistoryTab({ attempts, attemptsLoading }) {
  const [selectedTest, setSelectedTest] = useState(null);

  if (attemptsLoading) {
    return <div className="profile-content">Loading your test history...</div>;
  }

  if (!attempts || attempts.length === 0) {
    return (
      <div className="profile-content">
        <h2>Test History</h2>
        <NothingFound
          imageSrc="/src/assets/sad_cloud.png"
          title="No tests taken yet"
          message="Start your learning journey by taking your first exam!"
          actionLabel="Start Your First Test"
          to="/reading"
        />
      </div>
    );
  }

  // Sắp xếp theo ngày giảm dần
  const sorted = [...attempts].sort(
    (a, b) =>
      new Date(b.createdAt || b.dateTaken) -
      new Date(a.createdAt || a.dateTaken)
  );

  return (
    <div className="profile-content">
      <h2>Test History</h2>

      <div className="test-history-grid">
        {sorted.map((a) => {
          const date =
            a.dateTaken ||
            a.submittedAt ||
            a.createdAt ||
            new Date().toISOString();
          return (
            <div
              key={a.attemptId}
              className="test-card enhanced"
              onClick={() => setSelectedTest(a)}
            >
              <div className="date-badge">
                <CalendarDays size={14} />
                <span>
                  {new Date(date).toLocaleDateString("en-US", {
                    month: "short",
                    day: "numeric",
                  })}
                </span>
              </div>

              <div className="test-card-content">
                <h3>{a.examName}</h3>
                <span className={`badge-${a.examType?.toLowerCase()}`}>
                  {a.examType}
                </span>
                <p>
                  Score: <strong>{a.totalScore || 0}</strong>
                </p>
                <p className="subtext">{formatRelativeTime(date)}</p>
              </div>
            </div>
          );
        })}
      </div>

      <PopupBase
        title={selectedTest?.examName || ""}
        icon={ClipboardList}
        show={!!selectedTest}
        width="600px"
        onClose={() => setSelectedTest(null)}
      >
        {selectedTest && (
          <div className="test-detail-popup">
            <p className="exam-type">{selectedTest.examType}</p>

            <div className="test-detail-grid">
              <div>
                <strong>Attempt ID:</strong>
                <span>{selectedTest.attemptId}</span>
              </div>
              <div>
                <strong>Date Taken:</strong>
                <span>{formatFullDateVietnam(selectedTest.createdAt)}</span>
              </div>
              <div>
                <strong>Total Score:</strong>
                <span>{selectedTest.totalScore || 0}</span>
              </div>
            </div>
          </div>
        )}
      </PopupBase>
    </div>
  );
}
