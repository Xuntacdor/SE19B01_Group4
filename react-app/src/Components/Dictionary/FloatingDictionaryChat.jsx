import React, { useState } from "react";
import { BookOpen, ChevronDown, ChevronUp } from "lucide-react";
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
        setError(`Không tìm thấy từ "${query}".`);
      else setError("Lỗi tra từ, vui lòng thử lại.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.wrapper}>
      {/* Floating button */}
      {!isOpen && (
        <button className={styles.floatingBtn} onClick={() => setIsOpen(true)}>
          <BookOpen size={24} />
          <span>Tra từ vựng</span>
        </button>
      )}

      {/* Chat box */}
      {isOpen && (
        <div className={styles.chatBox}>
          <div className={styles.header}>
            <span>Tra từ vựng</span>
            <button onClick={() => setIsOpen(false)}>
              <ChevronDown size={20} />
            </button>
          </div>

          <div className={styles.body}>
            {loading ? (
              <p className={styles.hint}>Đang tra từ...</p>
            ) : error ? (
              <p className={styles.error}>{error}</p>
            ) : result ? (
              <div className={styles.result}>
                <p>
                  <strong>Từ:</strong> {result.term}
                </p>
                <p>
                  <strong>Nghĩa:</strong> {result.meaning || "-"}
                </p>
                <p>
                  <strong>Ví dụ:</strong> {result.example || "-"}
                </p>
                {result.audio && (
                  <button
                    className={styles.audioBtn}
                    onClick={() => new Audio(result.audio).play()}
                  >
                    🔊 Nghe phát âm
                  </button>
                )}
              </div>
            ) : (
              <p className={styles.hint}>
                Bạn hãy nhập từ hoặc cụm từ tiếng Việt, YouPass sẽ gợi ý cụm từ
                tiếng Anh tương ứng.
              </p>
            )}
          </div>

          <div className={styles.footer}>
            <input
              type="text"
              placeholder="Nhập từ tại đây..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              maxLength={50}
            />
            <button onClick={handleSearch}>Tra từ</button>
          </div>
          <div className={styles.limit}>Giới hạn: {query.length}/50 ký tự</div>
        </div>
      )}
    </div>
  );
}
