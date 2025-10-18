import React from "react";
import styles from "./LoadingComponent.module.css";

export default function LoadingComponent({ text = "Loading, please wait..." }) {
  return (
    <div className={styles.loadingContainer}>
      <div className={styles.spinner}></div>
      <p className={styles.loadingText}>{text}</p>
    </div>
  );
}
