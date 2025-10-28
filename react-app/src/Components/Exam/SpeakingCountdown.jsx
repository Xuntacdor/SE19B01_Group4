import React, { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import "./SpeakingCountdown.css";

export default function SpeakingCountdown({ phase, onComplete }) {
  const [time, setTime] = useState(phase === "preparing" ? 10 : 60);

  useEffect(() => {
    if (phase === "idle") return;
    setTime(phase === "preparing" ? 10 : 60);
    const timer = setInterval(() => {
      setTime((t) => {
        if (t <= 1) {
          clearInterval(timer);
          onComplete();
          return 0;
        }
        return t - 1;
      });
    }, 1000);
    return () => clearInterval(timer);
  }, [phase]);

  const message =
    phase === "preparing"
      ? "Hãy chuẩn bị sẵn sàng"
      : phase === "recording"
      ? "Đang ghi âm..."
      : "";

  return (
    <AnimatePresence>
      {phase !== "idle" && (
        <motion.div
          className="countdown-overlay"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
        >
          <motion.div
            className="countdown-box"
            initial={{ scale: 0.9, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            exit={{ scale: 0.9, opacity: 0 }}
          >
            <h2 className="countdown-title">{message}</h2>
            <div className="countdown-timer">
              {phase === "recording" && <span className="dot" />}
              <p>{time}s</p>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
