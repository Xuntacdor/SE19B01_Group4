import React, { useState, useEffect, useMemo } from "react";
import {
  BookOpen,
  ChevronDown,
  Loader2,
  AlertCircle,
  Info,
  PlusCircle,
  XCircle,
} from "lucide-react";
import * as WordApi from "../../Services/WordApi";
import * as VocabGroupApi from "../../Services/VocabGroupApi";
import Popup from "../../Components/Dictionary/PopUp";
import styles from "./FloatingDictionaryChat.module.css";

export default function FloatingDictionaryChat({ user: propUser }) {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [showPopup, setShowPopup] = useState(false);
  const [groups, setGroups] = useState([]);
  const [selectedGroupId, setSelectedGroupId] = useState("");
  const [showAddedHint, setShowAddedHint] = useState(false);

  // ✅ Lấy user từ prop hoặc localStorage
  const user = useMemo(() => {
    if (propUser) return propUser;
    try {
      const stored = localStorage.getItem("user");
      return stored ? JSON.parse(stored) : { userId: null };
    } catch {
      return { userId: null };
    }
  }, [propUser]);
  const userId = user?.userId;

  // ✅ Load group của user
  useEffect(() => {
    if (!userId) return;
    let active = true;
    (async () => {
      try {
        const res = await VocabGroupApi.getByUser(userId);
        if (active) setGroups(res.data);
      } catch {
        if (active) setGroups([]);
      }
    })();
    return () => {
      active = false;
    };
  }, [userId]);

  // ✅ Gửi request lookup AI
  const handleSearch = async () => {
    if (!query.trim()) return;
    setLoading(true);
    setError("");
    setResult(null);
    try {
      const res = await WordApi.lookupAI({
        word: query,
        context: "",
        userEssay: "",
      });
      setResult(res.data);
      setShowAddedHint(false);
    } catch (err) {
      console.error(err);
      setError("AI lookup failed. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  // ✅ Add từ vào group
  const handleAddToGroup = async () => {
    if (!selectedGroupId) {
      alert("Please select a group first.");
      return;
    }
    try {
      const wordId = result.wordId || result.WordId || result.id;
      if (!wordId) return;
      await VocabGroupApi.addWordToGroup(selectedGroupId, wordId);
      setShowPopup(false);
      setShowAddedHint(true);
    } catch (err) {
      console.error(err);
      alert("Failed to add word to group.");
    }
  };

  // ✅ Hiển thị kết quả dịch
  const renderResult = () => {
    if (!result) return null;

    const detected = (result.detected_language || "").toLowerCase();
    const isVN = detected === "vietnamese";

    const translation = isVN
      ? result.englishTranslation || "-"
      : result.vietnameseTranslation || "-";

    const label = isVN
      ? "🇬🇧 English Translation"
      : "🇻🇳 Vietnamese Translation";

    return (
      <div className={styles.result}>
        <p>
          <b>Từ:</b> {result.term}
        </p>
        <p>
          <b>{label}:</b> {translation}
        </p>
        {result.example && (
          <p>
            <b>Ví dụ (EN):</b> {result.example}
          </p>
        )}

        <button className={styles.addBtn} onClick={() => setShowPopup(true)}>
          <PlusCircle size={16} style={{ marginRight: 6 }} /> Add to Group
        </button>

        {showAddedHint && (
          <p
            style={{
              color: "green",
              fontSize: "13px",
              marginTop: "8px",
              fontWeight: 500,
            }}
          >
            ✓ Added to your group successfully.
          </p>
        )}
      </div>
    );
  };

  return (
    <div className={styles.wrapper}>
      {!isOpen && (
        <button className={styles.floatingBtn} onClick={() => setIsOpen(true)}>
          <BookOpen size={22} />
          <span>AI Dictionary</span>
        </button>
      )}

      {isOpen && (
        <div className={styles.chatBox}>
          <div className={styles.header}>
            <span>AI Dictionary</span>
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
              renderResult()
            ) : (
              <p className={styles.hint}>
                <Info size={16} style={{ marginRight: 5 }} />
                Enter a word to get its translation.
              </p>
            )}
          </div>

          <div className={styles.footer}>
            <input
              type="text"
              placeholder="Enter word here..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleSearch()}
              maxLength={50}
            />
            <button onClick={handleSearch}>Search</button>
          </div>

          <div className={styles.limit}>
            Limit: {query.length}/50 characters
          </div>
        </div>
      )}

      {/* Popup chọn group */}
      {showPopup && (
        <Popup
          title="Select Vocabulary Group"
          onClose={() => setShowPopup(false)}
          actions={
            <>
              <button onClick={handleAddToGroup} disabled={!selectedGroupId}>
                <PlusCircle size={18} /> Add
              </button>
              <button
                className={styles.cancelBtn}
                onClick={() => setShowPopup(false)}
              >
                <XCircle size={18} /> Cancel
              </button>
            </>
          }
        >
          {groups.length === 0 ? (
            <p style={{ color: "gray", textAlign: "center" }}>
              You don't have any vocabulary groups yet.
            </p>
          ) : (
            <select
              value={selectedGroupId || ""}
              onChange={(e) => setSelectedGroupId(Number(e.target.value))}
              className={styles.selectBox}
            >
              <option value="">-- Select group --</option>
              {groups.map((g) => (
                <option key={g.groupId} value={g.groupId}>
                  {g.groupname}
                </option>
              ))}
            </select>
          )}
        </Popup>
      )}
    </div>
  );
}
