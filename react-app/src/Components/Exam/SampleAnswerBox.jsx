import React from "react";
import AudioTextBox from "../Common/AudioTextBox";
import styles from "./SampleAnswerBox.module.css";

export default function SampleAnswerBox({ text }) {
  return (
    <div className={styles.sampleWrapper}>
      <AudioTextBox text={text} />
    </div>
  );
}
