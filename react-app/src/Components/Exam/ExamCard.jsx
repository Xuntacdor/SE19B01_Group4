import React from "react";
import styles from "./ExamCard.module.css";
import coverImg from "../../assets/image.png";

export default function ReadingCard({ exam, onTake }) {
  const hasBackgroundImage = exam.backgroundImageUrl && exam.backgroundImageUrl.trim() !== "";

  return (
    <div className={styles.card}>
      {/* Phần trên: 2/3 card với background image */}
      <div 
        className={styles.topSection}
        style={hasBackgroundImage ? {
          backgroundImage: `url(${exam.backgroundImageUrl})`,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
          backgroundRepeat: 'no-repeat',
        } : {}}
      >
        {hasBackgroundImage && <div className={styles.overlay}></div>}
        
        {!hasBackgroundImage && (
          <img className={styles.cover} src={coverImg} alt={exam.examName} />
        )}
        <div className={styles.coverBadge}>IELTS</div>
      </div>

      {/* Phần dưới: 1/3 card với nền trắng cho text */}
      <div className={styles.bottomSection}>
        <div className={styles.title}>{exam.examName}</div>
        <div className={styles.metaRow}>
          <span className={styles.metaLabel}>Type:</span>
          <span className={styles.metaValue}>{exam.examType}</span>
        </div>
        <button className={styles.takeBtn} onClick={onTake}>
          Take Test
        </button>
      </div>
    </div>
  );
}
