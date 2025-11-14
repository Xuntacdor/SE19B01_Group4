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

  // Load user
  const user = useMemo(() => {
    if (propUser) return propUser;
    try {
      return JSON.parse(localStorage.getItem("user")) || { userId: null };
    } catch {
      return { userId: null };
    }
  }, [propUser]);

  const userId = user?.userId;

  // Load user's vocab groups
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

  // Lookup AI
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

  // Add to group (auto-create if wordId == 0)
  const handleAddToGroup = async () => {
    if (!selectedGroupId) {
      alert("Please select a group first.");
      return;
    }

    try {
      let wordId = result.wordId || result.WordId || 0;

      // Auto-create if word not in DB
      if (!wordId || wordId === 0) {
        const createRes = await WordApi.add({
          term: result.term,
          meaning: result.meaning || "",
          audio: result.audio || null,
          example: result.example || "",
          groupIds: [],
        });

        wordId = createRes.data.wordId;
      }

      await VocabGroupApi.addWordToGroup(selectedGroupId, wordId);

      setShowPopup(false);
      setShowAddedHint(true);
    } catch (err) {
      console.error(err);
      alert("Failed to add word to group.");
    }
  };

  // ======================
  // Render result fixed
  // ======================
  const renderResult = () => {
    if (!result) return null;

    // Detect language robustly
    const detected =
      result.detected_language ||
      result.detectedLanguage ||
      result.language ||
      result.lang ||
      "";

    const isVN = detected.toLowerCase().includes("vietnam");

    let translation = "-";
    let label = "";

    // Case 1: AI lookup response
    if (result.englishTranslation || result.vietnameseTranslation) {
      label = isVN ? "🇬🇧 English Translation" : "🇻🇳 Vietnamese Translation";

      translation = isVN
        ? result.englishTranslation || "-"
        : result.vietnameseTranslation || "-";
    }

    // Case 2: WordDto from database (meaning field)
    if (result.meaning && translation === "-") {
      label = "Meaning";
      translation = result.meaning;
    }

    return (
      <div className={styles.result}>
        <p>
          <b>Từ:</b> {result.term}
        </p>

        {label && (
          <p>
            <b>{label}:</b> {translation}
          </p>
        )}

        {result.example && (
          <p>
            <b>Ví dụ (EN):</b> {result.example}
          </p>
        )}

        <button className={styles.addBtn} onClick={() => setShowPopup(true)}>
          <PlusCircle size={16} style={{ marginRight: 6 }} /> Add to Group
        </button>

        {showAddedHint && (
          <p style={{ color: "green", fontSize: "13px", marginTop: "8px" }}>
            ✓ Added to your group successfully.
          </p>
        )}
      </div>
    );
  };

  // ======================

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

      {/* Group popup */}
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
