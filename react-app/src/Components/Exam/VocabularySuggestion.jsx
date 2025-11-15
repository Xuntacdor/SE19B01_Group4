import React from "react";
import styles from "./VocabularySuggestion.module.css";
import { Volume2 } from "lucide-react";

const playAudio = (text) => {
  const utterance = new SpeechSynthesisUtterance(text);
  utterance.lang = "en-US";
  window.speechSynthesis.cancel();
  window.speechSynthesis.speak(utterance);
};

export default function VocabularySuggestion({ vocab }) {
  if (!vocab) return null;

  const levels = [
    { key: "basic", label: "Basic Vocabulary" },
    { key: "intermediate", label: "Intermediate Vocabulary" },
    { key: "advanced", label: "Advanced Vocabulary" },
  ];

  return (
    <div className={styles.vocabContainer}>
      {levels.map(({ key, label }) => (
        <div key={key} className={styles.levelSection}>
          <h3>{label}</h3>

          <div className={styles.wordGrid}>
            {(vocab[key] || []).map((item, i) => {
              // fallback: nếu API trả string cũ, không crash
              const term = typeof item === "string" ? item : item.term;
              const vn = typeof item === "string" ? "" : item.vn;

              return (
                <div key={i} className={styles.wordCard}>
                  <div className={styles.wordHeader}>
                    <button
                      className={styles.audioBtn}
                      onClick={() => playAudio(term)}
                    >
                      <Volume2 size={18} />
                    </button>

                    <div className={styles.textBlock}>
                      <span className={styles.en}>{term}</span>
                      <span className={styles.vn}>{vn}</span>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}
