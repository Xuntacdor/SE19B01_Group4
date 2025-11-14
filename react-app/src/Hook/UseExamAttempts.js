import { useState, useEffect } from "react";
import { getExamAttemptsByUser } from "../Services/ExamApi";
import * as SpeakingApi from "../Services/SpeakingApi";
import * as WritingApi from "../Services/WritingApi";

/**
 * Custom Hook: useExamAttempts
 *
 * - Nếu chỉ truyền userId  → Dashboard mode (tính IELTS stats)
 * - Nếu truyền userId + examId → chỉ lấy attempts của bài đó (ví dụ SpeakingResultPage)
 */
export default function useExamAttempts(userId, examId = null) {
  const [attempts, setAttempts] = useState([]);
  const [stats, setStats] = useState({
    Reading: 0,
    Listening: 0,
    Writing: 0,
    Speaking: 0,
    Overall: 0,
  });
  const [loading, setLoading] = useState(true);

  // ============================================================
  // 📌 Fetch tất cả attempts theo user
  //     - Nếu examId != null → chỉ filter đúng bài cần xem result
  // ============================================================
  useEffect(() => {
    if (!userId) return;
    let isActive = true;

    const loadAttempts = async () => {
      try {
        setLoading(true);

        let data = await getExamAttemptsByUser(userId);

        // ❗ API đôi khi trả string → parse
        if (typeof data === "string") {
          try {
            data = JSON.parse(data);
          } catch {
            console.warn("⚠️ Invalid JSON from ExamAttempt API");
            data = [];
          }
        }

        // ❗ Nếu đang ở trang result: chỉ lấy đúng exam
        if (examId) {
          data = data.filter((x) => x.examId === examId);
        }

        // ============================================================
        // 📌 Enrich từng exam với finalScore chuẩn
        //      - Reading/Listening lấy từ totalScore/score
        //      - Writing lấy từ AI feedback
        //      - Speaking lấy từ AI feedback (finalOverall hoặc average)
        // ============================================================
        const enriched = await Promise.all(
          data.map(async (item) => {
            let score =
              item.totalScore ??
              item.score ??
              item.averageOverall ??
              item.feedback?.overall ??
              0;

            try {
              if (item.examType === "Speaking") {
                const res = await SpeakingApi.getFeedback(item.examId, userId);

                score =
                  res.finalOverall ??
                  res.averageOverall ??
                  (res.feedbacks?.length
                    ? res.feedbacks.reduce(
                        (sum, f) => sum + (f.overall ?? 0),
                        0
                      ) / res.feedbacks.length
                    : 0);
              }

              if (item.examType === "Writing") {
                const res = await WritingApi.getFeedback(item.examId, userId);

                score = res.averageOverall ?? res.feedbacks?.[0]?.overall ?? 0;
              }
            } catch (e) {
              console.warn(`⚠️ Cannot fetch feedback for ${item.examType}`, e);
            }

            return {
              ...item,
              finalScore: score,
            };
          })
        );

        if (isActive) {
          setAttempts(enriched);
        }
      } catch (err) {
        console.error("❌ Load attempts failed:", err);
      } finally {
        if (isActive) setLoading(false);
      }
    };

    loadAttempts();
    return () => (isActive = false);
  }, [userId, examId]);

  // ============================================================
  // 📌 Dashboard Mode → tính IELTS stats
  //      - Nếu examId != null -> SKIP (vì đây là trang result)
  // ============================================================
  useEffect(() => {
    if (examId) return; // ⛔ Skip khi tính điểm cho trang result
    if (attempts.length === 0) return;

    const grouped = {
      Reading: [],
      Listening: [],
      Writing: [],
      Speaking: [],
    };

    attempts.forEach((a) => {
      if (grouped[a.examType]) grouped[a.examType].push(a.finalScore ?? 0);
    });

    const avg = (arr) =>
      arr.length ? arr.reduce((a, b) => a + b, 0) / arr.length : 0;

    const roundIELTS = (score) => {
      const base = Math.floor(score);
      const decimal = score - base;
      if (decimal < 0.25) return base;
      if (decimal < 0.75) return base + 0.5;
      return base + 1;
    };

    const reading = roundIELTS(avg(grouped.Reading));
    const listening = roundIELTS(avg(grouped.Listening));
    const writing = roundIELTS(avg(grouped.Writing));
    const speaking = roundIELTS(avg(grouped.Speaking));

    const overall = roundIELTS((reading + listening + writing + speaking) / 4);

    setStats({
      Reading: reading,
      Listening: listening,
      Writing: writing,
      Speaking: speaking,
      Overall: overall,
    });
  }, [attempts, examId]);

  return {
    attempts,
    stats,
    loading,
  };
}
