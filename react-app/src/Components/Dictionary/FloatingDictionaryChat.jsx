import React, { useState } from "react";
import {
  BookOpen,
  ChevronDown,
  ChevronUp,
  Volume2,
  Loader2,
  AlertCircle,
  Info,
} from "lucide-react";
import * as WordApi from "../../Services/WordApi";
import styles from "./FloatingDictionaryChat.module.css";

export default function FloatingDictionaryChat() {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSearch = async () => {
    if (!query.trim()) return;
    setLoading(true);
    setError("");
    try {
      const res = await WordApi.lookup(query);
      setResult(res.data);
    } catch (err) {
      setResult(null);
      if (err.response?.status === 404)
        setError(`Could not find the word "${query}".`);
      else setError("Word lookup error, please try again.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.wrapper}>
      {!isOpen && (
        <button className={styles.floatingBtn} onClick={() => setIsOpen(true)}>
          <BookOpen size={22} />
          <span>Look up word</span>
        </button>
      )}

      {isOpen && (
        <div className={styles.chatBox}>
          <div className={styles.header}>
            <span>Look up word</span>
            <button onClick={() => setIsOpen(false)}>
              <ChevronDown size={20} />
            </button>
          </div>

          <div className={styles.body}>
            {loading ? (
              <p className={styles.hint}>
                <Loader2 className={styles.spin} size={18} /> Searching...
              </p>
            ) : error ? (
              <p className={styles.error}>
                <AlertCircle size={16} style={{ marginRight: 6 }} />
                {error}
              </p>
            ) : result ? (
              <div className={styles.result}>
                <p>
                  <strong>Word:</strong> {result.term}
                </p>
                <p>
                  <strong>Meaning:</strong> {result.meaning || "-"}
                </p>
                <p>
                  <strong>Example:</strong> {result.example || "-"}
                </p>
                {result.audio && (
                  <button
                    className={styles.audioBtn}
                    onClick={() => new Audio(result.audio).play()}
                  >
                    <Volume2 size={16} style={{ marginRight: 5 }} />
                    Play pronunciation
                  </button>
                )}
              </div>
            ) : (
              <p className={styles.hint}>
                <Info size={16} style={{ marginRight: 5 }} />
                Please enter a word or phrase to look up its meaning.
              </p>
            )}
          </div>

          <div className={styles.footer}>
            <input
              type="text"
              placeholder="Enter word here..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              maxLength={50}
            />
            <button onClick={handleSearch}>Search</button>
          </div>
          <div className={styles.limit}>
            Limit: {query.length}/50 characters
          </div>
        </div>
      )}
    </div>
  );
}
