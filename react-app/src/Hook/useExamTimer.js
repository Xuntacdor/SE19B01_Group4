import { useEffect, useState, useCallback } from "react";

/**
 * Custom hook quản lý thời gian đếm ngược cho bài thi.
 * @param {number} initialMinutes - thời lượng ban đầu (phút)
 * @param {boolean} isStopped - dừng khi đã nộp hoặc hết giờ
 */
export default function useExamTimer(initialMinutes = 0, isStopped = false) {
  const [timeLeft, setTimeLeft] = useState(initialMinutes * 60);

  useEffect(() => {
    if (!timeLeft || isStopped) return;
    const timer = setInterval(() => {
      setTimeLeft((t) => Math.max(0, t - 1));
    }, 1000);
    return () => clearInterval(timer);
  }, [timeLeft, isStopped]);

  const formatTime = useCallback((sec) => {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${m}:${s < 10 ? "0" + s : s}`;
  }, []);

  const resetTimer = useCallback((newMinutes) => {
    setTimeLeft(newMinutes * 60);
  }, []);

  const isFinished = timeLeft <= 0;

  return { timeLeft, formatTime, resetTimer, isFinished };
}
