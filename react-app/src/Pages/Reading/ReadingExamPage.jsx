// ReadingExamPage.jsx
import React, { useEffect, useState, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { marked } from "marked";
import { submitReadingAttempt } from "../../Services/ReadingApi";
import ExamMarkdownRenderer from "../../Components/Exam/ExamMarkdownRenderer";
import { Highlighter, Trash2, Pencil } from "lucide-react";
import styles from "./ReadingExamPage.module.css";

// ---------- Markdown config for the PASSAGE ----------
marked.setOptions({
  gfm: true,
  breaks: true,
  mangle: false,
  headerIds: false,
});

// Remove [H*id]...[/H] wrappers and render full Markdown (so headings, lists, breaks work)
function passageMarkdownToHtml(raw) {
  if (!raw) return "";
    const cleaned = String(raw).replace(
      /\[H(?:\*([^\]]*))?\]([\s\S]*?)\[\/H\]/g,
      (_match, _id, inner) => inner
    );
  return marked.parse(cleaned);
}

// ----------------- DOM Range serialization helpers -----------------
/**
 * Get path of a node relative to root, as array of child indexes.
 */
function getNodePath(root, node) {
  const path = [];
  let cur = node;
  while (cur && cur !== root) {
    const parent = cur.parentNode;
    if (!parent) break;
    let idx = 0;
    for (let i = 0; i < parent.childNodes.length; i++) {
      if (parent.childNodes[i] === cur) {
        idx = i;
        break;
      }
    }
    path.unshift(idx);
    cur = parent;
  }
  return path;
}

/**
 * Get node from root using path (array of child indexes).
 */
function getNodeFromPath(root, path) {
  let node = root;
  for (let i = 0; i < path.length; i++) {
    if (!node || !node.childNodes) return null;
    const idx = path[i];
    node = node.childNodes[idx];
    if (!node) return null;
  }
  return node;
}

/**
 * Serialize a Range into an object referencing node paths and offsets.
 */
function serializeRange(root, range) {
  return {
    startPath: getNodePath(root, range.startContainer),
    startOffset: range.startOffset,
    endPath: getNodePath(root, range.endContainer),
    endOffset: range.endOffset,
  };
}

/**
 * Restore a Range from serialized data relative to root.
 */
function restoreRange(root, serialized) {
  try {
    const startNode = getNodeFromPath(root, serialized.startPath);
    const endNode = getNodeFromPath(root, serialized.endPath);
    if (!startNode || !endNode) return null;
    const r = document.createRange();

    // Normalize offsets to node type
    const startMax =
      startNode.nodeType === Node.TEXT_NODE
        ? startNode.nodeValue.length
        : startNode.childNodes.length;
    const endMax =
      endNode.nodeType === Node.TEXT_NODE
        ? endNode.nodeValue.length
        : endNode.childNodes.length;

    r.setStart(startNode, Math.min(serialized.startOffset, startMax));
    r.setEnd(endNode, Math.min(serialized.endOffset, endMax));
    return r;
  } catch (err) {
    console.error("restoreRange failed", err);
    return null;
  }
}

/**
 * Compare two node-path arrays lexicographically to compute order in the DOM.
 * Returns -1, 0, 1
 */
function comparePaths(a, b) {
  const la = a.length;
  const lb = b.length;
  const l = Math.max(la, lb);
  for (let i = 0; i < l; i++) {
    const va = a[i] ?? -1;
    const vb = b[i] ?? -1;
    if (va < vb) return -1;
    if (va > vb) return 1;
  }
  return 0;
}
// ----------------- End helpers -----------------

export default function ReadingExamPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const { exam, tasks, duration } = state || {};

  const [currentTask, setCurrentTask] = useState(0);
  const [submitted, setSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [timeLeft, setTimeLeft] = useState(duration ? duration * 60 : 0);
  const [answers, setAnswers] = useState({});
  const formRef = useRef(null);

  const [highlightMode, setHighlightMode] = useState(false);

  /**
   * highlights: array of {
   *   id: string,
   *   serializedRange: { startPath, startOffset, endPath, endOffset },
   *   text: string
   * }
   */
  const [highlights, setHighlights] = useState([]);
  const [contextMenu, setContextMenu] = useState(null);
  const [pendingSelection, setPendingSelection] = useState(null);
  const passageContentRef = useRef(null);

  // Countdown
  useEffect(() => {
    if (!timeLeft || submitted) return;
    const timer = setInterval(
      () => setTimeLeft((t) => Math.max(0, t - 1)),
      1000
    );
    return () => clearInterval(timer);
  }, [timeLeft, submitted]);

  const formatTime = (sec) => {
    const m = Math.floor(sec / 60);
    const s = sec % 60;
    return `${m}:${s < 10 ? "0" + s : s}`;
  };

  // Capture form changes
  const handleChange = (e) => {
    const { name, value, type, checked, multiple, options, dataset } = e.target;
    if (!name) return;

    if (type === "checkbox") {
      const limit = parseInt(dataset.limit || "0", 10);
      const group = formRef.current?.querySelectorAll(
        `input[name="${name}"][type="checkbox"]`
      );
      const checkedInGroup = Array.from(group || []).filter((el) => el.checked);

      if (checked && limit && checkedInGroup.length > limit) {
        e.preventDefault();
        e.target.checked = false;
        alert(`You can only select ${limit} option${limit > 1 ? "s" : ""}.`);
        return;
      }

      const selected = checkedInGroup.map((el) => el.value);
      setAnswers((prev) => ({ ...prev, [name]: selected }));
      return;
    }

    if (type === "radio") {
      if (checked) setAnswers((prev) => ({ ...prev, [name]: value }));
      return;
    }

    if (multiple) {
      const selected = Array.from(options)
        .filter((opt) => opt.selected)
        .map((opt) => opt.value);
      setAnswers((prev) => ({ ...prev, [name]: selected }));
      return;
    }

    setAnswers((prev) => ({ ...prev, [name]: value }));
  };

  // Submit attempt
  const handleSubmit = (e) => {
    e?.preventDefault();
    if (isSubmitting) return;

    const structuredAnswers = (tasks || []).map((task) => {
      const prefix = `${task.readingId}_q`;
      const questionKeys = Object.keys(answers)
        .filter((k) => k.startsWith(prefix))
        .sort((a, b) => {
          const na = parseInt(a.split("_q")[1]) || 0;
          const nb = parseInt(b.split("_q")[1]) || 0;
          return na - nb;
        });

      const taskAnswers = {};
      questionKeys.forEach((key) => {
        const val = answers[key];
        if (Array.isArray(val) && val.length > 0) taskAnswers[key] = val;
        else if (typeof val === "string" && val.trim() !== "")
          taskAnswers[key] = val;
        else taskAnswers[key] = "_";
      });

      return { SkillId: task.readingId, Answers: taskAnswers };
    });

    setIsSubmitting(true);
    const jsonString = JSON.stringify(structuredAnswers);
    const attempt = {
      examId: exam.examId,
      startedAt: new Date().toISOString(),
      answers: jsonString,
    };

    submitReadingAttempt(attempt)
      .then((res) => {
        const attempt = res.data;
        navigate("/reading/result", {
          state: {
            attemptId: attempt.attemptId,
            examName: attempt.examName,
            isWaiting: true,
          },
        });
        setSubmitted(true);
      })
      .catch((err) => {
        console.error("❌ Submit failed:", err);
        alert(`Failed to submit your reading attempt.\n\n${jsonString}`);
      })
      .finally(() => setIsSubmitting(false));
  };

  // For nav buttons: count [!num]
  const getQuestionCount = (readingQuestion) => {
    if (!readingQuestion) return 0;
    const numMarkers = readingQuestion.match(/\[!num\]/g);
    return numMarkers ? numMarkers.length : 0;
  };

  // Toggle highlight mode
  const toggleHighlightMode = () => {
    setHighlightMode((prev) => !prev);
    setContextMenu(null);
    setPendingSelection(null);
    const sel = window.getSelection();
    if (sel) sel.removeAllRanges();
  };

  // Handle selection (mouse-only)
  const handleTextSelection = (e) => {
    if (!highlightMode) return;

    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return;

    const range = selection.getRangeAt(0);
    if (range.collapsed) return;

    const passageRoot = passageContentRef.current;
    if (!passageRoot || !passageRoot.contains(range.commonAncestorContainer)) {
      return;
    }

    // If selection intersects an existing highlight, show clear menu
    let node = range.commonAncestorContainer;
    if (node.nodeType === Node.TEXT_NODE) node = node.parentElement;
    let highlightElement = null;
    while (node && node !== passageRoot) {
      if (
        node.nodeType === Node.ELEMENT_NODE &&
        node.getAttribute &&
        node.getAttribute("data-highlight-id")
      ) {
        highlightElement = node;
        break;
      }
      node = node.parentNode;
    }

    if (highlightElement) {
      const highlightId = highlightElement.getAttribute("data-highlight-id");
      setContextMenu({
        x: e.clientX,
        y: e.clientY,
        highlightId,
        type: "existing",
      });
      selection.removeAllRanges();
      return;
    }

    // New selection: serialize range & store pending selection
    try {
      const serialized = serializeRange(passageRoot, range);
      const text = selection.toString().trim();

      setPendingSelection({
        serializedRange: serialized,
        text,
      });

      setContextMenu({
        x: e.clientX,
        y: e.clientY,
        type: "new",
      });
    } catch (err) {
      console.error("Selection serialization failed", err);
    }
  };

  // Apply highlight from pendingSelection
  const applyHighlight = () => {
    if (!pendingSelection) return;
    const passageRoot = passageContentRef.current;
    if (!passageRoot) return;

    const highlightId = `highlight-${Date.now()}-${Math.random()
      .toString(36)
      .substr(2, 9)}`;

    const highlightData = {
      id: highlightId,
      serializedRange: pendingSelection.serializedRange,
      text: pendingSelection.text,
    };

    setHighlights((prev) => {
      // Prevent duplicate identical serialized range
      const exists = prev.some(
        (h) =>
          JSON.stringify(h.serializedRange) ===
          JSON.stringify(highlightData.serializedRange)
      );
      if (exists) return prev;
      return [...prev, highlightData];
    });

    const sel = window.getSelection();
    if (sel) sel.removeAllRanges();
    setPendingSelection(null);
    setContextMenu(null);
  };

  // Clear highlight
  const clearHighlight = (highlightId) => {
    setHighlights((prev) => prev.filter((h) => h.id !== highlightId));
    setContextMenu(null);
  };

  // Apply highlights to DOM whenever highlights or currentTask change
  useEffect(() => {
    const passageRoot = passageContentRef.current;
    if (!passageRoot) return;

    // Remove spans for highlights no longer present
    const existingSpans = Array.from(
      passageRoot.querySelectorAll(`span[data-highlight-id]`)
    );
    const validIds = new Set(highlights.map((h) => h.id));
    existingSpans.forEach((sp) => {
      const id = sp.getAttribute("data-highlight-id");
      if (!validIds.has(id)) {
        const parent = sp.parentNode;
        while (sp.firstChild) parent.insertBefore(sp.firstChild, sp);
        parent.removeChild(sp);
      }
    });

    // Sort highlights by start path, apply from end to start so DOM changes don't shift later ranges
    const sorted = [...highlights].sort((a, b) => {
      const cmp = comparePaths(
        a.serializedRange.startPath,
        b.serializedRange.startPath
      );
      return cmp === 0
        ? comparePaths(a.serializedRange.endPath, b.serializedRange.endPath)
        : cmp;
    }).reverse();

    sorted.forEach((h) => {
      // skip if already applied
      if (passageRoot.querySelector(`span[data-highlight-id="${h.id}"]`))
        return;

      const range = restoreRange(passageRoot, h.serializedRange);
      if (!range) {
        // fallback: try to find first text occurrence
        if (h.text) {
          try {
            const plain = passageRoot.innerText || passageRoot.textContent || "";
            const idx = plain.indexOf(h.text);
            if (idx !== -1) {
              let charCount = 0;
              let startNode = null;
              let startOffset = 0;
              const walker = document.createTreeWalker(
                passageRoot,
                NodeFilter.SHOW_TEXT,
                null,
                false
              );
              while (walker.nextNode()) {
                const tn = walker.currentNode;
                const nextCount = charCount + (tn.nodeValue?.length || 0);
                if (idx >= charCount && idx < nextCount) {
                  startNode = tn;
                  startOffset = idx - charCount;
                  break;
                }
                charCount = nextCount;
              }
              if (startNode) {
                const r = document.createRange();
                r.setStart(startNode, startOffset);
                r.setEnd(
                  startNode,
                  Math.min(startNode.nodeValue.length, startOffset + h.text.length)
                );
                const frag = r.extractContents();
                const span = document.createElement("span");
                span.className = styles.highlightedText;
                span.setAttribute("data-highlight-id", h.id);
                span.appendChild(frag);
                r.insertNode(span);
              }
            }
          } catch (err) {
            console.warn("Fallback highlight failed", err);
          }
        }
        return;
      }

      if (range.collapsed) return;

      // Skip if range already inside highlight
      let ancestor = range.commonAncestorContainer;
      if (ancestor.nodeType === Node.TEXT_NODE) ancestor = ancestor.parentElement;
      if (ancestor?.closest(`span[data-highlight-id]`)) {
        return;
      }

      try {
        const extracted = range.extractContents();
        const span = document.createElement("span");
        span.className = styles.highlightedText;
        span.setAttribute("data-highlight-id", h.id);
        span.appendChild(extracted);
        range.insertNode(span);
      } catch (err) {
        console.warn("extractContents failed; trying safer per-node wrap", err);
        // Try a safer approach: wrap text nodes inside restored range individually
        try {
          const safeRange = restoreRange(passageRoot, h.serializedRange);
          if (!safeRange) return;
          const walker = document.createTreeWalker(
            passageRoot,
            NodeFilter.SHOW_TEXT,
            null,
            false
          );
          const nodes = [];
          while (walker.nextNode()) {
            const tn = walker.currentNode;
            const r2 = document.createRange();
            r2.selectNodeContents(tn);
            if (
              r2.compareBoundaryPoints(Range.END_TO_START, safeRange) <= 0 &&
              r2.compareBoundaryPoints(Range.START_TO_END, safeRange) >= 0
            ) {
              nodes.push(tn);
            }
          }
          nodes.forEach((tn) => {
            const parent = tn.parentNode;
            const span = document.createElement("span");
            span.className = styles.highlightedText;
            span.setAttribute("data-highlight-id", h.id);
            span.textContent = tn.nodeValue;
            parent.replaceChild(span, tn);
          });
        } catch (err2) {
          console.error("Failed to apply highlight on fallback", err2);
        }
      }
    });
  }, [highlights, currentTask]);

  // Mouse-only selection handlers
  useEffect(() => {
    const handleMouseUp = (e) => {
      if (highlightMode) {
        setTimeout(() => handleTextSelection(e), 10);
      }
    };

    if (highlightMode) {
      document.addEventListener("mouseup", handleMouseUp);
      return () => {
        document.removeEventListener("mouseup", handleMouseUp);
      };
    }
  }, [highlightMode]);

  // Close context menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (contextMenu) {
        if (e.target.closest(`.${styles.contextMenu}`)) {
          return;
        }
        if (contextMenu.type === "new") {
          const selection = window.getSelection();
          selection.removeAllRanges();
          setPendingSelection(null);
        }
        setContextMenu(null);
      }
    };

    if (contextMenu) {
      const timeoutId = setTimeout(() => {
        document.addEventListener("click", handleClickOutside);
      }, 100);

      return () => {
        clearTimeout(timeoutId);
        document.removeEventListener("click", handleClickOutside);
      };
    }
  }, [contextMenu]);

  if (!exam)
    return (
      <div className={styles.fullscreenCenter}>
        <h2>No exam selected</h2>
        <button className={styles.backBtn} onClick={() => navigate("/reading")}>
          ← Back
        </button>
      </div>
    );

  if (submitted)
    return (
      <div className={styles.fullscreenCenter}>
        <h3>✅ Reading Test Submitted!</h3>
        <p>Your answers have been recorded successfully.</p>
        <button className={styles.backBtn} onClick={() => navigate("/reading")}>
          ← Back to Reading List
        </button>
      </div>
    );

  const currentTaskData = (tasks || [])[currentTask];
  const questionCount = getQuestionCount(currentTaskData?.readingQuestion);

  // Helper for question bar
  const isAnswered = (readingId, qNumber) => {
    const key = `${readingId}_q${qNumber}`;
    const val = answers[key];
    if (Array.isArray(val)) return val.length > 0;
    if (typeof val === "string") return val.trim() !== "";
    return false;
  };

  return (
    <div className={styles.examWrapper}>
      {/* Header */}
      <div className={styles.topHeader}>
        <button className={styles.backBtn} onClick={() => navigate("/reading")}>
          ← Back
        </button>
        <h2 className={styles.examTitle}>{exam.examName}</h2>
        <div className={styles.timer}>⏰ {formatTime(timeLeft)}</div>
      </div>

      <div className={styles.mainContent}>
        {/* Left: Passage */}
        <div className={styles.leftPanel}>
          {currentTaskData?.passageTitle && (
            <div className={styles.passageHeader}>
              <h3 className={styles.passageTitle}>
                {currentTaskData.passageTitle}
              </h3>
            </div>
          )}
          <div
            ref={passageContentRef}
            className={`${styles.passageContent} ${
              highlightMode ? styles.highlightMode : ""
            }`}
            onClick={(e) => {
              const highlightElement = e.target.closest(`[data-highlight-id]`);
              if (highlightElement) {
                e.preventDefault();
                e.stopPropagation();
                const highlightId = highlightElement.getAttribute("data-highlight-id");
                setContextMenu({
                  x: e.clientX,
                  y: e.clientY,
                  highlightId,
                  type: "existing",
                });
              }
            }}
            dangerouslySetInnerHTML={{
              __html: passageMarkdownToHtml(
                currentTaskData?.readingContent || ""
              ),
            }}
          />
        </div>

        {/* Right: Questions */}
        <div className={styles.rightPanel}>
          <div className={styles.rightPanelDock}>
            <form ref={formRef} onChange={handleChange} onInput={handleChange}>
              {currentTaskData?.readingQuestion ? (
                <ExamMarkdownRenderer
                  markdown={currentTaskData.readingQuestion}
                  showAnswers={false}
                  skillId={currentTaskData.readingId}
                />
              ) : (
                <div className={styles.questionSection}>
                  <div
                    style={{
                      padding: "40px",
                      textAlign: "center",
                      color: "#666",
                    }}
                  >
                    <h3>No Questions Found</h3>
                    <p>
                      This reading test doesn't have any questions configured
                      yet.
                    </p>
                  </div>
                </div>
              )}
            </form>
          </div>
        </div>
      </div>

      {/* Bottom Nav */}
      <div className={styles.bottomNavigation}>
        <div className={styles.navScrollContainer}>
          {(tasks || []).map((task, taskIndex) => {
            const count = getQuestionCount(task.readingQuestion);
            return (
              <div
                key={task.readingId}
                className={styles.navSection}
                onClick={() => {
                  setCurrentTask(taskIndex);
                  document
                    .querySelector(`.${styles.examWrapper}`)
                    ?.scrollTo({ top: 0, behavior: "smooth" });
                }}
                role="button"
              >
                <div
                  className={`${styles.navSectionTitle} ${
                    currentTask === taskIndex
                      ? styles.navSectionTitleActive
                      : ""
                  }`}
                >
                  Part {taskIndex + 1}
                </div>
                {currentTask === taskIndex && (
                  <div className={styles.navQuestions}>
                    {Array.from({ length: count }, (_, qIndex) => {
                      const qNum = qIndex + 1;
                      const answered = isAnswered(task.readingId, qNum);
                      return (
                        <button
                          type="button"
                          key={`${taskIndex}-${qIndex}`}
                          className={[
                            styles.navButton,
                            answered
                              ? styles.completedNavButton
                              : styles.unansweredNavButton,
                            qIndex === 0 ? styles.activeNavButton : "",
                          ].join(" ")}
                          title={answered ? "Answered" : "Unanswered"}
                          onClick={(e) => {
                            e.stopPropagation();
                            setCurrentTask(taskIndex);
                            document
                              .querySelector(`.${styles.examWrapper}`)
                              ?.scrollTo({ top: 0, behavior: "smooth" });
                          }}
                        >
                          {qNum}
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}
        </div>

        <button
          className={`${styles.highlightButton} ${
            highlightMode ? styles.highlightButtonActive : ""
          }`}
          onClick={toggleHighlightMode}
          title={
            highlightMode ? "Disable Highlight Mode" : "Enable Highlight Mode"
          }
        >
          <Highlighter size={18} style={{ marginRight: "6px" }} />
          {highlightMode ? "Highlighting" : "Highlight"}
        </button>

        <button
          className={styles.completeButton}
          onClick={handleSubmit}
          disabled={isSubmitting}
        >
          {isSubmitting ? "Submitting..." : "Complete"}
        </button>
      </div>

      {/* Context Menu */}
      {contextMenu && (
        <div
          className={styles.contextMenu}
          style={{
            left: `${contextMenu.x}px`,
            top: `${contextMenu.y}px`,
          }}
          onClick={(e) => e.stopPropagation()}
        >
          {contextMenu.type === "new" ? (
            <button className={styles.contextMenuItem} onClick={applyHighlight}>
              <Pencil size={16} className={styles.contextMenuIcon} />
              Highlight
            </button>
          ) : (
            <button
              className={styles.contextMenuItem}
              onClick={() => clearHighlight(contextMenu.highlightId)}
            >
              <Trash2 size={16} className={styles.contextMenuIcon} />
              Clear Highlight
            </button>
          )}
        </div>
      )}
    </div>
  );
}
