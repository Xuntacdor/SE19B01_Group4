import React, { useState, useEffect, useMemo } from "react";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import styles from "./DashboardUser.module.css";
import { useNavigate } from "react-router-dom";
import NothingFound from "../../Components/Nothing/NothingFound";
import * as AuthApi from "../../Services/AuthApi";
import { getSubmittedDays } from "../../Services/ExamApi";
import useExamAttempts from "../../Hook/UseExamAttempts";
import { isDaySubmitted } from "../../utils/date";
import * as SpeakingApi from "../../Services/SpeakingApi";
import * as WritingApi from "../../Services/WritingApi";
import sadcloud from "../../assets/sad_cloud.png";
import {
  Book,
  Headphones,
  BarChart2,
  CheckCircle,
  XCircle,
  Pen,
  Mic,
} from "lucide-react";

/* ================================
   Configs for stats display
================================ */
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
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
  const itemsPerPage = 3;

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
    function roundIELTS(score) {
      if (score == null || isNaN(score)) return "-";
      return Math.round(score * 2) / 2; // round to nearest 0.5
    }
    const fetchScores = async () => {
      setLoadingScores(true);
      const rows = await Promise.all(
        attempts.map(async (a) => {
          let score = "-";
          try {
            if (a.examType === "Speaking") {
              try {
                const fullFeedback = await SpeakingApi.getFeedback(
                  a.examId,
                  userId
                );

                let finalOverall = "-";

                if (fullFeedback?.averageOverall) {
                  finalOverall = roundIELTS(
                    Number(fullFeedback.averageOverall)
                  );
                } else if (fullFeedback?.feedbacks?.length > 0) {
                  const avg =
                    fullFeedback.feedbacks.reduce(
                      (sum, f) => sum + (f.overall ?? 0),
                      0
                    ) / fullFeedback.feedbacks.length;

                  finalOverall = roundIELTS(avg);
                }

                score = finalOverall;
              } catch (err) {
                console.warn("Failed to fetch speaking score", err);
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
            console.warn(` Failed to get feedback for ${a.examType}`, err);
          }

          return {
            statusIcon: a.submittedAt ? (
              <CheckCircle size={18} color="#28a745" />
            ) : (
              <XCircle size={18} color="#dc3545" />
            ),
            examName: a.examName,
            examType: a.examType,
            date: a.submittedAt
              ? new Date(a.submittedAt).toLocaleDateString("en-GB")
              : "In progress",
            score,
            attemptId: a.attemptId,
            examId: a.examId,
          };
        })
      );

      // mới nhất lên trên
      setHistoryData(rows.reverse());
      setLoadingScores(false);
    };

    fetchScores();
  }, [attempts, userId]);

  const hasHistory = historyData.length > 0;

  return (
    <AppLayout title="Summary of your hard work" sidebar={<GeneralSidebar />}>
      <div className={styles.dashboardContent}>
        {/* ===== Calendar Section ===== */}
        {/* ===== Compact Calendar Section ===== */}
        <div className={`${styles.card} ${styles.calendarCard}`}>
          <div className={styles.cardHeader}>
            <h3 className={styles.cardTitle}>Your dedication chart</h3>
          </div>

          <div className={styles.compactCalendar}>
            <div className={styles.calendarMonthTitle}>
              {monthName} / {year}
            </div>

            {/* Weekday row */}
            <div className={styles.calendarWeekdays}>
              {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((d) => (
                <div key={d}>{d}</div>
              ))}
            </div>

            {/* Grid */}
            <div className={styles.calendarDaysGrid}>
              {Array.from({ length: startOffset }).map((_, i) => (
                <div key={`e-${i}`} className={styles.emptyDay}></div>
              ))}

              {Array.from({ length: daysInMonth }, (_, i) => {
                const day = i + 1;
                const submitted = isDaySubmitted(
                  day,
                  month,
                  year,
                  submittedDays
                );

                return (
                  <div
                    key={day}
                    className={`${styles.dayCell} ${
                      submitted ? styles.submittedDay : ""
                    }`}
                  >
                    {day}
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* ===== Stats Section ===== */}
        <div className={styles.goalsWrapper}>
          <div className={styles.statsSection}>
            <h3 className={styles.sectionTitle}>Outcome Statistics</h3>
            <p className={styles.chartSubtitle}>
              Band scores scaled on a 0–9 range
            </p>

            <div className={styles.chartContainer}>
              <ResponsiveContainer width="100%" height={260}>
                <BarChart
                  data={[
                    { skill: "Reading", value: stats.Reading ?? 0 },
                    { skill: "Listening", value: stats.Listening ?? 0 },
                    { skill: "Writing", value: stats.Writing ?? 0 },
                    { skill: "Speaking", value: stats.Speaking ?? 0 },
                    { skill: "Overall", value: stats.Overall ?? 0 },
                  ]}
                  margin={{ top: 10, right: 20, left: -10, bottom: 5 }}
                >
                  <CartesianGrid strokeDasharray="3 3" stroke="#ddd" />
                  <XAxis
                    dataKey="skill"
                    tick={{ fill: "#333", fontSize: 12 }}
                    interval={0}
                  />
                  <YAxis
                    domain={[0, 9]}
                    tick={{ fill: "#666", fontSize: 12 }}
                  />
                  <Tooltip />
                  <Bar dataKey="value" fill="#4c8ffb" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
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
          ) : !hasHistory ? (
            <NothingFound
              imageSrc={sadcloud}
              title="No practice history"
              message="You have not done any exercises yet! Choose the appropriate form and practice now!"
              actionLabel="Do your homework now!"
              to="/reading"
            />
          ) : (
            <div className={styles.historyTable}>
              <div className={`${styles.historyRow} ${styles.historyHeader}`}>
                <div>Status</div>
                <div>Exam Name</div>
                <div>Type</div>
                <div>Date</div>
                <div>Score</div>
              </div>

              {/* Luôn render 5 hàng: dữ liệu + hàng trống */}
              {(() => {
                const rowsToRender = [...paginatedData];
                const emptyCount = itemsPerPage - rowsToRender.length;

                for (let i = 0; i < emptyCount; i++) {
                  rowsToRender.push({ empty: true, _emptyId: `empty-${i}` });
                }

                return rowsToRender.map((r, i) => {
                  if (r.empty) {
                    // Hàng trống chỉ để giữ layout
                    return (
                      <div
                        className={styles.historyRow}
                        key={r._emptyId ?? `empty-${i}`}
                      >
                        <div></div>
                        <div></div>
                        <div></div>
                        <div></div>
                        <div></div>
                      </div>
                    );
                  }

                  return (
                    <div className={styles.historyRow} key={r.attemptId ?? i}>
                      <div>{r.statusIcon}</div>

                      <div
                        className={styles.examLink}
                        onClick={() => {
                          if (r.examType === "Reading") {
                            navigate("/reading/result", {
                              state: {
                                attemptId: r.attemptId,
                                examName: r.examName,
                              },
                            });
                          } else if (r.examType === "Listening") {
                            navigate("/listening/result", {
                              state: {
                                attemptId: r.attemptId,
                                examName: r.examName,
                              },
                            });
                          } else if (r.examType === "Writing") {
                            navigate("/writing/result", {
                              state: {
                                examId: r.examId,
                                userId,
                                exam: { examName: r.examName },
                                mode: "full",
                                originalAnswers: null,
                                isWaiting: false,
                                attemptId: r.attemptId,
                              },
                            });
                          } else if (r.examType === "Speaking") {
                            const examId =
                              r.examId ??
                              r.speaking?.examId ??
                              r.speakingExamId;

                            navigate("/speaking/result", {
                              state: {
                                examId,
                                examName: r.examName,
                              },
                            });
                          }
                        }}
                      >
                        {r.examName}
                      </div>

                      <div>{r.examType}</div>
                      <div>{r.date}</div>
                      <div>{r.score}</div>
                    </div>
                  );
                });
              })()}
            </div>
          )}
          {totalPages > 1 && (
            <div className={styles.pagination}>
              {(() => {
                const pages = [];

                // Luôn căn 5 trang quanh currentPage
                let start = currentPage - 2;
                let end = currentPage + 2;

                // Không cho nhỏ hơn 1
                if (start < 1) {
                  end += 1 - start;
                  start = 1;
                }

                // Không cho lớn hơn totalPages
                if (end > totalPages) {
                  start -= end - totalPages;
                  end = totalPages;
                }

                // Giữ đúng 5 trang
                start = Math.max(1, start);
                end = Math.min(totalPages, end);

                while (end - start < 4 && end < totalPages) end++;
                while (end - start < 4 && start > 1) start--;

                for (let p = start; p <= end; p++) {
                  pages.push(p);
                }

                return pages.map((page) => (
                  <button
                    key={page}
                    onClick={() => setCurrentPage(page)}
                    className={`${styles.pageButton} ${
                      currentPage === page ? styles.activePage : ""
                    }`}
                  >
                    {page}
                  </button>
                ));
              })()}
            </div>
          )}
        </div>
      </div>
    </AppLayout>
  );
}
