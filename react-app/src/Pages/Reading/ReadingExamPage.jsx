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
  breaks: true, // single newlines -> <br>
  mangle: false,
  headerIds: false,
});

// Remove [H*id]...[/H] wrappers and render full Markdown (so headings, lists, breaks work)
function passageMarkdownToHtml(raw) {
  if (!raw) return "";
  const cleaned = String(raw).replace(
    /\[H(?:\*([^\]]*))?\]([\s\S]*?)\[\/H\]/g,
    (_match, _id, inner) => inner // keep inner text; drop the wrapper entirely
  );
  return marked.parse(cleaned);
}

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
  const [highlights, setHighlights] = useState([]);
  const [contextMenu, setContextMenu] = useState(null);
  const [pendingSelection, setPendingSelection] = useState(null);
  const passageContentRef = useRef(null);
  const [useReadingLayout, setUseReadingLayout] = useState(false);

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

  // Highlight functionality
  const toggleHighlightMode = () => {
    setHighlightMode((prev) => !prev);
    setContextMenu(null);
  };

  const handleTextSelection = (e) => {
    if (!highlightMode) return;

    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0) return;

    const range = selection.getRangeAt(0);
    const selectedText = selection.toString().trim();

    if (!selectedText) return;

    // Check if selection is within the passage content
    const passageElement = passageContentRef.current;
    if (
      !passageElement ||
      !passageElement.contains(range.commonAncestorContainer)
    ) {
      return;
    }

    // Check if selection is already inside a highlight
    let node = range.commonAncestorContainer;
    let highlightElement = null;

    // Check if the selection is inside a highlighted element
    if (node.nodeType === Node.TEXT_NODE) {
      node = node.parentElement;
    }

    while (node && node !== passageElement) {
      if (
        node.nodeType === Node.ELEMENT_NODE &&
        node.getAttribute &&
        node.getAttribute("data-highlight-id")
      ) {
        highlightElement = node;
        break;
      }
      node = node.parentElement || node.parentNode;
    }

    // If already highlighted, show clear option
    if (highlightElement) {
      const highlightId = highlightElement.getAttribute("data-highlight-id");
      setContextMenu({
        x: e.clientX || (e.touches && e.touches[0]?.clientX) || 0,
        y: e.clientY || (e.touches && e.touches[0]?.clientY) || 0,
        highlightId: highlightId,
        type: "existing",
      });
      return;
    }

    // Store the selection for highlighting later
    setPendingSelection({ text: selectedText });

    // Show context menu with Highlight option
    setContextMenu({
      x: e.clientX || (e.touches && e.touches[0]?.clientX) || 0,
      y: e.clientY || (e.touches && e.touches[0]?.clientY) || 0,
      type: "new",
    });

    // Don't clear selection yet - keep it visible
  };

  const applyHighlight = () => {
    if (!pendingSelection) return;

    const selectedText = pendingSelection.text;

    // Create a unique ID for this highlight
    const highlightId = `highlight-${Date.now()}-${Math.random()
      .toString(36)
      .substr(2, 9)}`;

    // Store highlight data
    const highlightData = {
      id: highlightId,
      text: selectedText,
    };

    setHighlights((prev) => {
      // Check if this exact text is already highlighted
      const exists = prev.some((h) => h.text === selectedText);
      if (exists) return prev;
      return [...prev, highlightData];
    });

    // Clear selection and pending selection
    const selection = window.getSelection();
    if (selection) {
      selection.removeAllRanges();
    }
    setPendingSelection(null);
    setContextMenu(null);
  };

  const clearHighlight = (highlightId) => {
    setHighlights((prev) => prev.filter((h) => h.id !== highlightId));
    setContextMenu(null);
  };

  // Apply highlights to the passage content - only highlight first occurrence of each text
  const applyHighlightsToHtml = (html, highlightsList) => {
    if (!highlightsList || highlightsList.length === 0) return html;

    let processedHtml = html;

    // Process each highlight - only highlight the first unhighlighted occurrence
    highlightsList.forEach((highlight) => {
      const textToHighlight = highlight.text.trim();

      // Create a flexible regex that handles whitespace variations
      // Escape special regex characters
      const escapedText = textToHighlight.replace(
        /[.*+?^${}()|[\]\\]/g,
        "\\$&"
      );
      // Replace all whitespace (spaces, newlines, tabs) with flexible whitespace matcher
      const flexiblePattern = escapedText.replace(/\s+/g, "\\s+");

      // Try to find the text with flexible whitespace matching
      const regex = new RegExp(flexiblePattern, "gi");
      let match;
      let found = false;

      while ((match = regex.exec(processedHtml)) !== null && !found) {
        const matchIndex = match.index;
        const beforeMatch = processedHtml.substring(0, matchIndex);

        // Check if we're inside an HTML tag
        const lastOpenTag = beforeMatch.lastIndexOf("<");
        const lastCloseTag = beforeMatch.lastIndexOf(">");
        if (lastOpenTag > lastCloseTag) {
          continue; // Skip if inside a tag
        }

        // Check if we're already inside a highlight span
        const lastHighlightOpen = beforeMatch.lastIndexOf(
          `<span class="${styles.highlightedText}"`
        );
        const lastHighlightClose = beforeMatch.lastIndexOf("</span>");
        if (lastHighlightOpen > lastHighlightClose) {
          continue; // Skip if already highlighted
        }

        // Found a valid position - apply highlight
        const before = processedHtml.substring(0, matchIndex);
        const after = processedHtml.substring(matchIndex + match[0].length);
        processedHtml =
          before +
          `<span class="${styles.highlightedText}" data-highlight-id="${highlight.id}">${match[0]}</span>` +
          after;
        found = true;
        break; // Only highlight first occurrence
      }
    });

    return processedHtml;
  };

  // Handle mouse up for text selection
  useEffect(() => {
    const handleMouseUp = (e) => {
      if (highlightMode) {
        // Small delay to ensure selection is captured
        setTimeout(() => {
          handleTextSelection(e);
        }, 10);
      }
    };

    if (highlightMode) {
      document.addEventListener("mouseup", handleMouseUp);
      return () => document.removeEventListener("mouseup", handleMouseUp);
    }
  }, [highlightMode, highlights]);

  // Close context menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (contextMenu) {
        // Don't close if clicking on the context menu itself
        if (e.target.closest(`.${styles.contextMenu}`)) {
          return;
        }
        // Close the context menu and clear pending selection if it was a new selection
        if (contextMenu.type === "new") {
          const selection = window.getSelection();
          if (selection) {
            selection.removeAllRanges();
          }
          setPendingSelection(null);
        }
        setContextMenu(null);
      }
    };

    if (contextMenu) {
      // Use a small delay to avoid closing immediately when opening
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

  // ===== helper ONLY for the question bar =====
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
              // Check if clicked element or its parent has the highlight data attribute
              const target = e.target;
              const highlightElement = target.closest(`[data-highlight-id]`);
              if (highlightElement) {
                e.preventDefault();
                e.stopPropagation();
                const highlightId =
                  highlightElement.getAttribute("data-highlight-id");
                if (highlightId) {
                  setContextMenu({
                    x: e.clientX,
                    y: e.clientY,
                    highlightId: highlightId,
                    type: "existing",
                  });
                }
              }
            }}
            dangerouslySetInnerHTML={{
              __html: applyHighlightsToHtml(
                passageMarkdownToHtml(currentTaskData?.readingContent || ""),
                highlights
              ),
            }}
          />
        </div>

        {/* Right: Questions */}
        <div className={styles.rightPanel}>
          {/* Dock fixes clipping & alignment */}
          <div className={styles.rightPanelDock}>
            <form ref={formRef} onChange={handleChange} onInput={handleChange}>
              {currentTaskData?.readingQuestion ? (
                <ExamMarkdownRenderer
                  markdown={currentTaskData.readingQuestion}
                  showAnswers={false} // explanations hidden on exam
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

      {/* Bottom Nav — replaced with listening-like behavior */}
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
