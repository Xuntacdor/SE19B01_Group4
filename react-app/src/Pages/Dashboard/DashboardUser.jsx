import React, { useState, useEffect, useMemo } from "react";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import styles from "./DashboardUser.module.css";
import {
  Book,
  Headphones,
  BarChart2,
  CheckCircle,
  XCircle,
  Pen,
  Mic,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import NothingFound from "../../Components/Nothing/NothingFound";

import * as AuthApi from "../../Services/AuthApi";
import { getSubmittedDays } from "../../Services/ExamApi";
import useExamAttempts from "../../Hook/UseExamAttempts";
import { isDaySubmitted } from "../../utils/date";
import * as SpeakingApi from "../../Services/SpeakingApi";
import * as WritingApi from "../../Services/WritingApi";

/* ================================
   Configs for stats display
================================ */
const STAT_CONFIGS = [
  {
    key: "Reading",
    label: "Reading",
    color: "#fd7e14",
    icon: <Book size={18} color="#fd7e14" />,
    bg: "readingBg",
  },
  {
    key: "Listening",
    label: "Listening",
    color: "#28a745",
    icon: <Headphones size={18} color="#28a745" />,
    bg: "listeningBg",
  },
  {
    key: "Writing",
    label: "Writing",
    color: "#dc3545",
    icon: <Pen size={18} color="#dc3545" />,
    bg: "writingBg",
  },
  {
    key: "Speaking",
    label: "Speaking",
    color: "#6f42c1",
    icon: <Mic size={18} color="#6f42c1" />,
    bg: "speakingBg",
  },
  {
    key: "Overall",
    label: "Overall",
    color: "#007bff",
    icon: <BarChart2 size={18} color="#007bff" />,
    bg: "overallBg",
  },
];

export default function DashboardUser() {
  const navigate = useNavigate();

  // ===== Calendar logic =====
  const today = new Date();
  const year = today.getFullYear();
  const month = today.getMonth();
  const monthName = today.toLocaleString("default", { month: "long" });

  const firstDay = new Date(year, month, 1).getDay(); // Sun=0
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const startOffset = firstDay === 0 ? 6 : firstDay - 1; // Mon=0

  const weeks = useMemo(() => {
    const days = Array.from({ length: startOffset }, () => null).concat(
      Array.from({ length: daysInMonth }, (_, i) => i + 1)
    );
    const result = [];
    for (let i = 0; i < days.length; i += 7) result.push(days.slice(i, i + 7));
    return result;
  }, [month, year]);

  // ===== State =====
  const [userId, setUserId] = useState(null);
  const [submittedDays, setSubmittedDays] = useState([]);
  const [historyData, setHistoryData] = useState([]);
  const [loadingScores, setLoadingScores] = useState(false);

  // ===== Pagination logic =====
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5;

  const totalPages = Math.ceil(historyData.length / itemsPerPage);
  const paginatedData = useMemo(() => {
    const start = (currentPage - 1) * itemsPerPage;
    const end = start + itemsPerPage;
    return historyData.slice(start, end);
  }, [historyData, currentPage]);

  // ===== Get current user =====
  useEffect(() => {
    AuthApi.getMe()
      .then((meRes) => setUserId(meRes.data.userId))
      .catch((err) => console.error("Failed to fetch user:", err));
  }, []);

  // ===== Submitted days =====
  useEffect(() => {
    if (!userId) return;
    getSubmittedDays(userId)
      .then((days) => setSubmittedDays(days))
      .catch((err) => console.error("Failed to fetch submitted days:", err));
  }, [userId]);

  // ===== History + Stats =====
  const { attempts, stats } = useExamAttempts(userId);

  useEffect(() => {
    if (!attempts || attempts.length === 0 || !userId) return;

    const fetchScores = async () => {
      setLoadingScores(true);
      const rows = await Promise.all(
        attempts.map(async (a) => {
          let score = "-";

          try {
            if (a.examType === "Speaking") {
              // === Giống cách SpeakingTest lấy overall ===
              try {
                const res = await SpeakingApi.getFeedbackBySpeakingId(
                  a.speakingId || a.attemptId || a.examId,
                  userId
                );

                if (res?.feedback?.overall != null)
                  score = Number(res.feedback.overall).toFixed(1);
                else if (res?.overall != null)
                  score = Number(res.overall).toFixed(1);
                else throw new Error("No speaking feedback found");
              } catch {
                // Fallback: lấy theo examId
                const altRes = await SpeakingApi.getFeedback(
                  a.examId || a.attemptId,
                  userId
                );

                if (altRes?.feedbacks?.length) {
                  const match = altRes.feedbacks.find(
                    (f) => String(f.speakingId) === String(a.speakingId)
                  );
                  const pick =
                    match || altRes.feedbacks[altRes.feedbacks.length - 1];
                  if (pick?.overall != null)
                    score = Number(pick.overall).toFixed(1);
                  else if (altRes.averageOverall != null)
                    score = Number(altRes.averageOverall).toFixed(1);
                } else if (altRes?.averageOverall != null)
                  score = Number(altRes.averageOverall).toFixed(1);
              }
            } else if (a.examType === "Writing") {
              const feedbackRes = await WritingApi.getFeedback(
                a.examId || a.attemptId,
                userId
              );
              if (feedbackRes?.averageOverall)
                score = feedbackRes.averageOverall.toFixed(1);
              else if (feedbackRes?.feedbacks?.length > 0)
                score = feedbackRes.feedbacks[0].overall?.toFixed(1);
            } else {
              // Reading & Listening
              score = a.totalScore?.toFixed(1) ?? a.score?.toFixed(1) ?? "-";
            }
          } catch (err) {
            console.warn(`⚠️ Failed to get feedback for ${a.examType}`, err);
          }

          return [
            a.submittedAt ? (
              <CheckCircle size={18} color="#28a745" />
            ) : (
              <XCircle size={18} color="#dc3545" />
            ),
            a.examName,
            a.examType,
            a.submittedAt
              ? new Date(a.submittedAt).toLocaleDateString("en-GB")
              : "In progress",
            score,
          ];
        })
      );

      setHistoryData(rows);
      setLoadingScores(false);
    };

    fetchScores();
  }, [attempts, userId]);

  return (
    <AppLayout title="Summary of your hard work" sidebar={<GeneralSidebar />}>
      <div className={styles.dashboardContent}>
        {/* ===== Calendar Section ===== */}
        <div className={`${styles.card} ${styles.calendarCard}`}>
          <div className={styles.cardHeader}>
            <h3 className={styles.cardTitle}>Your dedication for studying</h3>
            <div className={styles.status}>
              <span className={styles.statusDot} />
              <span className={styles.statusText}>Submitted</span>
            </div>
          </div>

          <div className={styles.calendar}>
            <div className={styles.calendarHeader}>
              <h4 className={styles.monthYear}>
                {monthName} {year}
              </h4>
            </div>

            <div className={styles.calendarGrid}>
              <div />
              {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((d) => (
                <div key={d} className={styles.calendarWeekday}>
                  {d}
                </div>
              ))}
              {weeks.map((w, wi) => (
                <React.Fragment key={wi}>
                  <div className={styles.calendarWeekLabel}>Week {wi + 1}</div>
                  {w.map((d, di) => (
                    <div
                      key={di}
                      className={`${styles.calendarDay} ${
                        isDaySubmitted(d, month, year, submittedDays)
                          ? styles.submitted
                          : ""
                      }`}
                    >
                      {d || ""}
                    </div>
                  ))}
                </React.Fragment>
              ))}
            </div>
          </div>
        </div>

        {/* ===== Stats Section ===== */}
        <div className={styles.goalsWrapper}>
          <div className={styles.statsSection}>
            <h3 className={styles.sectionTitle}>Outcome Statistics</h3>

            {STAT_CONFIGS.map(({ key, label, color, icon, bg }) => (
              <div key={key} className={styles.statItem}>
                <div className={`${styles.iconBox} ${styles[bg]}`}>{icon}</div>
                <span className={styles.statLabel}>{label}</span>
                <div className={styles.progressBar}>
                  <div
                    className={styles.progressFill}
                    style={{
                      width: `${(stats[key] / 9) * 100}%`,
                      background: color,
                    }}
                  />
                </div>
                <span className={styles.statValue}>
                  {stats[key]?.toFixed(1) ?? "0.0"}/9
                </span>
              </div>
            ))}
          </div>

          {/* ===== Banner Section ===== */}
          <div className={styles.bannerCard}>
            <div className={styles.bannerText}>
              <h4>🔥 Upgrade to VIP IELTS Access</h4>
              <p>
                Unlock AI-powered Writing & Speaking feedback, advanced
                analytics, and personalized progress tracking — all in one
                place.
              </p>
            </div>
            <button
              className={styles.bannerButton}
              onClick={() => navigate("/vipplans")}
            >
              Upgrade Now
            </button>
          </div>
        </div>

        {/* ===== History Section ===== */}
        <div className={`${styles.historySection} ${styles.card}`}>
          <h3 className={styles.sectionTitle}>Practice History</h3>

          {loadingScores ? (
            <div className={styles.stateText}>Fetching AI feedback...</div>
          ) : (
            <div className={styles.historyTable}>
              <div className={`${styles.historyRow} ${styles.historyHeader}`}>
                <div>Status</div>
                <div>Exam Name</div>
                <div>Type</div>
                <div>Date</div>
                <div>Score</div>
              </div>

              {paginatedData.length > 0 ? (
                paginatedData.map((r, i) => (
                  <div className={styles.historyRow} key={i}>
                    <div>{r[0]}</div>
                    <div>{r[1]}</div>
                    <div>{r[2]}</div>
                    <div>{r[3]}</div>
                    <div>{r[4]}</div>
                  </div>
                ))
              ) : (
                <NothingFound
                  imageSrc="/src/assets/sad_cloud.png"
                  title="No practice history"
                  message="You have not done any exercises yet! Choose the appropriate form and practice now!"
                  actionLabel="Do your homework now!"
                  to="/reading"
                />
              )}
            </div>
          )}

          {totalPages > 1 && (
            <div className={styles.pagination}>
              {Array.from({ length: totalPages }, (_, i) => i + 1).map(
                (page) => (
                  <button
                    key={page}
                    onClick={() => setCurrentPage(page)}
                    className={`${styles.pageButton} ${
                      currentPage === page ? styles.activePage : ""
                    }`}
                  >
                    {page}
                  </button>
                )
              )}
            </div>
          )}
        </div>
      </div>
    </AppLayout>
  );
}
