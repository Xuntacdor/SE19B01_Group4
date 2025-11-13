import React from "react";
import { Volume2 } from "lucide-react";
import styles from "./AudioTextBox.module.css";

export default function AudioTextBox({ text, label, className }) {
  if (!text) return null;

  const playAudio = () => {
    const utter = new SpeechSynthesisUtterance(text);
    utter.lang = "en-US";
    utter.rate = 1;
    utter.pitch = 1;
    window.speechSynthesis.cancel();
    window.speechSynthesis.speak(utter);
  };

  return (
    <div className={`${styles.audioBox} ${className || ""}`}>
      {label && <h4 className={styles.label}>{label}</h4>}
      <div className={styles.textRow}>
        <button className={styles.playBtn} onClick={playAudio}>
          <Volume2 size={18} />
        </button>
        <p className={styles.text}>{text}</p>
      </div>
    </div>
  );
}
