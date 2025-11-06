import { useState, useEffect } from "react";
import { getExamAttemptsByUser } from "../Services/ExamApi";
import * as SpeakingApi from "../Services/SpeakingApi";
import * as WritingApi from "../Services/WritingApi";

export default function useExamAttempts(userId) {
  const [attempts, setAttempts] = useState([]);
  const [stats, setStats] = useState({
    Reading: 0,
    Listening: 0,
    Writing: 0,
    Speaking: 0,
    Overall: 0,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // ====== Fetch attempts ======
  useEffect(() => {
    if (!userId) return;
    let isMounted = true;
    setLoading(true);
    setError(null);

    getExamAttemptsByUser(userId)
      .then(async (data) => {
        if (typeof data === "string") {
          try {
            data = JSON.parse(data);
          } catch {
            console.error("❌ Failed to parse ExamAttempt JSON");
            data = [];
          }
        }

        console.log("📘 Attempts from API:", data);

        // ✅ Enrich with feedback score for Speaking/Writing
        const enriched = await Promise.all(
          data.map(async (a) => {
            let score =
              a.totalScore ??
              a.score ??
              a.averageOverall ??
              a.feedback?.overall ??
              0;

            try {
              if (a.examType === "Speaking") {
                const res = await SpeakingApi.getFeedbackBySpeakingId(
                  a.speakingId || a.attemptId || a.examId,
                  userId
                );
                if (res?.feedback?.overall)
                  score = parseFloat(res.feedback.overall);
              } else if (a.examType === "Writing") {
                const res = await WritingApi.getFeedback(
                  a.examId || a.attemptId,
                  userId
                );
                if (res?.averageOverall) score = parseFloat(res.averageOverall);
                else if (res?.feedbacks?.[0]?.overall)
                  score = parseFloat(res.feedbacks[0].overall);
              }
            } catch (err) {
              console.warn(
                `⚠️ Failed to fetch feedback for ${a.examType}`,
                err.message || err
              );
            }

            return { ...a, finalScore: score };
          })
        );

        if (isMounted) setAttempts(enriched);
      })
      .catch((err) => isMounted && setError(err))
      .finally(() => isMounted && setLoading(false));

    return () => {
      isMounted = false;
    };
  }, [userId]);

  // ====== Compute IELTS band stats ======
  useEffect(() => {
    if (!attempts || attempts.length === 0) return;

    const grouped = { Reading: [], Listening: [], Writing: [], Speaking: [] };
    attempts.forEach((a) => {
      const score = a.finalScore ?? 0;
      if (grouped[a.examType]) grouped[a.examType].push(score);
    });

    const avg = (arr) =>
      arr.length > 0 ? arr.reduce((a, b) => a + b, 0) / arr.length : 0;

    // ✅ Official IELTS rounding rule
    const roundIELTS = (score) => {
      const floor = Math.floor(score);
      const decimal = score - floor;
      if (decimal < 0.25) return floor;
      if (decimal < 0.75) return floor + 0.5;
      return floor + 1;
    };

    // Apply rounding for each skill before computing overall
    const reading = roundIELTS(avg(grouped.Reading));
    const listening = roundIELTS(avg(grouped.Listening));
    const writing = roundIELTS(avg(grouped.Writing));
    const speaking = roundIELTS(avg(grouped.Speaking));

    const overallRaw = (reading + listening + writing + speaking) / 4;
    const overall = roundIELTS(overallRaw);

    console.log("🎯 Computed IELTS Stats:", {
      Reading: reading,
      Listening: listening,
      Writing: writing,
      Speaking: speaking,
      Overall: overall,
    });

    setStats({
      Reading: reading,
      Listening: listening,
      Writing: writing,
      Speaking: speaking,
      Overall: overall,
    });
  }, [attempts]);

  return { attempts, stats, loading, error };
}
