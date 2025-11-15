import React, { useEffect, useMemo, useState } from "react";
import styles from "./QuizPage.module.css";
import { CheckCircle, XCircle } from "lucide-react";

export default function QuizPage({ groupWords, onBack }) {
  const initialOrder = useMemo(
    () => groupWords.map((_, i) => i).sort(() => 0.5 - Math.random()),
    [groupWords]
  );

  const [queue, setQueue] = useState(initialOrder);
  const [cursor, setCursor] = useState(0);

  const [mastered, setMastered] = useState(new Set());
  const [failed, setFailed] = useState(new Set());

  const [questionNumber, setQuestionNumber] = useState(1);

  const [selected, setSelected] = useState(null);
  const [revealing, setRevealing] = useState(false);
  const [showMeaningRevealed, setShowMeaningRevealed] = useState(false);
  const [fadeOthers, setFadeOthers] = useState(false);

  const total = groupWords.length;
  const currentIndex = queue[cursor];

  // ========== FALLBACK WRONG OPTIONS ==========
  const fallbackWrong = [
    "an unrelated action",
    "a small object or device",
    "a kind of vehicle",
    "to move quickly without direction",
    "a piece of music",
    "a type of weather condition",
  ];

  // ========== CREATE OPTIONS ==========
  const makeOptions = (idx) => {
    if (idx == null || idx < 0)
      return { question: "", options: [], answer: "" };

    const word = groupWords[idx];

    let pool = groupWords
      .filter((w, i) => i !== idx && w.meaning?.trim())
      .map((w) => w.meaning);

    if (pool.length < 3) {
      const need = 3 - pool.length;
      pool = pool.concat(
        fallbackWrong.sort(() => 0.5 - Math.random()).slice(0, need)
      );
    }

    const wrong = Array.from(new Set(pool))
      .filter((m) => m !== word.meaning)
      .sort(() => 0.5 - Math.random())
      .slice(0, 3);

    const options = Array.from(new Set([...wrong, word.meaning])).sort(
      () => 0.5 - Math.random()
    );

    return {
      question: `What is the meaning of '${word.term}'?`,
      options,
      answer: word.meaning,
    };
  };

  const { question, options, answer } = useMemo(
    () => makeOptions(currentIndex),
    [currentIndex, groupWords]
  );

  // ========== AUTO RETURN WHEN DONE ==========
  useEffect(() => {
    if (cursor >= queue.length) {
      onBack(); // 🔥 Auto quay về dictionary
    }
  }, [cursor, queue.length, onBack]);

  // ========== NEXT STEP LOGIC ==========
  const goNext = (wasCorrect) => {
    if (wasCorrect) {
      setMastered((prev) => new Set(prev).add(currentIndex));
      setQuestionNumber((n) => n + 1);   // Only correct → count question
    } else {
      setQueue((q) => [...q, currentIndex]); // wrong → requeue
      setFailed((prev) => new Set(prev).add(currentIndex));
      // ❌ do NOT increase questionNumber
    }

    setCursor((c) => c + 1);
  };

const handleSkip = () => {
  // Skip: KHÔNG tính là sai → KHÔNG requeue
  setCursor((c) => c + 1);
  setQuestionNumber((n) => n + 1); 

  setSelected(null);
  setRevealing(false);
  setShowMeaningRevealed(false);
  setFadeOthers(false);
};

  const handleRevealMeaning = () => {
    if (revealing || selected) return;
    setShowMeaningRevealed(true);

    setTimeout(() => {
      setShowMeaningRevealed(false);
    }, 1200);
  };

  const handleAnswer = (opt) => {
    if (revealing || showMeaningRevealed) return;

    const correct = opt === answer;

    setSelected(opt);
    setRevealing(true);
    setFadeOthers(true);

    setTimeout(() => {
      setSelected(null);
      setRevealing(false);
      setFadeOthers(false);
      setShowMeaningRevealed(false);

      goNext(correct); // ✔ correct or wrong processed here
    }, 700);
  };

  return (
    <div className={styles.quizContainer}>
      <h2 className={styles.question}>{question}</h2>

      <button
        className={styles.revealBtn}
        onClick={handleRevealMeaning}
        disabled={!!selected || revealing || showMeaningRevealed}
      >
        Reveal meaning
      </button>

      <ul className={styles.optionsGrid}>
        {options.map((opt, idx) => {
          const isCorrect = selected && opt === answer;
          const isWrong = selected && opt === selected && opt !== answer;
          const isRevealed = showMeaningRevealed && opt === answer;
          const shouldFade =
            fadeOthers && !isCorrect && !isWrong && !isRevealed;

          return (
            <li key={idx} className={styles.optionItem}>
              <button
                className={`${styles.optionBtn}
                  ${isCorrect ? styles.correct : ""}
                  ${isWrong ? styles.wrong : ""}
                  ${isRevealed ? styles.revealed : ""}
                  ${shouldFade ? styles.faded : ""}
                `}
                onClick={() => handleAnswer(opt)}
                disabled={!!selected || revealing || showMeaningRevealed}
              >
                {opt}
                {selected &&
                  (isCorrect ? (
                    <CheckCircle className={styles.icon} color="#4ade80" />
                  ) : isWrong ? (
                    <XCircle className={styles.icon} color="#f87171" />
                  ) : null)}
              </button>
            </li>
          );
        })}
      </ul>

      <div className={styles.actionButtons}>
        <button
          className={styles.skipBtn}
          onClick={handleSkip}
          disabled={!!selected || revealing || showMeaningRevealed}
        >
          Skip this word
        </button>
      </div>

      <p className={styles.meta}>
        Question {Math.min(questionNumber, total)} of {total}
      </p>
    </div>
  );
}
