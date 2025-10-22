import React, {
  useState,
  useEffect,
  useRef,
  useMemo,
  useCallback,
} from "react";
import { Edit2, Loader2 } from "lucide-react";
import styles from "./ExamPopup.module.css";

export default function ExamSkillModal({
  exam,
  tasks,
  loading,
  onClose,
  onStartFullTest,
  onStartIndividual,
  skillType,
}) {
  const dropdownRef = useRef(null);
  const [selectedTaskId, setSelectedTaskId] = useState(null);
  const [isEditingTime, setIsEditingTime] = useState(false);

  // =========================
  // == Dynamic duration logic
  // =========================
  const getDefaultDuration = useCallback((skill) => {
    switch (skill) {
      case "Speaking":
        return 15;
      case "Listening":
        return 40;
      case "Reading":
      case "Writing":
        return 60;
      default:
        return 60;
    }
  }, []);

  const getDurationOptions = useCallback((skill) => {
    switch (skill) {
      case "Speaking":
        return [10, 12, 15, 18, 20];
      case "Listening":
        return [30, 35, 40, 45];
      case "Writing":
        return [45, 55, 60, 65];
      case "Reading":
        return [50, 55, 60, 65];
      default:
        return [30, 40, 60];
    }
  }, []);

  const [duration, setDuration] = useState(() => {
    const saved = localStorage.getItem(`${skillType}_duration`);
    return saved ? Number(saved) : getDefaultDuration(skillType);
  });

  useEffect(() => {
    const saved = localStorage.getItem(`${skillType}_duration`);
    if (saved) setDuration(Number(saved));
    else setDuration(getDefaultDuration(skillType));
  }, [skillType, getDefaultDuration]);

  const handleDurationChange = (min) => {
    setDuration(min);
    localStorage.setItem(`${skillType}_duration`, min);
    setIsEditingTime(false);
  };

  // =========================
  // == Handle click outside dropdown
  // =========================
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target)) {
        setIsEditingTime(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // =========================
  // == Helper functions
  // =========================
  const totalTasks = Array.isArray(tasks) ? tasks.length : 0;

  const taskTypes = useMemo(() => {
    if (!Array.isArray(tasks)) return "";
    const types = tasks.map(
      (t) =>
        t.readingType ||
        t.listeningType ||
        t.writingType ||
        t.speakingType ||
        "Unknown"
    );
    return [...new Set(types)].join(", ");
  }, [tasks]);

  const getDisplayOrder = useCallback(
    (t) => t.displayOrder ?? t.DisplayOrder ?? 0,
    []
  );
  const getTaskId = useCallback(
    (t) => t.readingId || t.listeningId || t.writingId || t.speakingId,
    []
  );
  const getTaskLabel = useCallback(
    (t) =>
      t.readingType ||
      t.listeningType ||
      t.writingType ||
      t.speakingType ||
      "Task",
    []
  );

  const getTaskDurationMinutes = useCallback(
    (t) => {
      const order = Number(getDisplayOrder(t));
      if (skillType === "Speaking") {
        // 3 phần Speaking: Part 1 (4-5'), Part 2 (3-4'), Part 3 (4-5')
        if (order === 1) return 5;
        if (order === 2) return 3;
        if (order === 3) return 5;
        return 4;
      }
      if (order === 1) return 20;
      if (order === 2) return 40;
      return 30;
    },
    [skillType, getDisplayOrder]
  );

  // =========================
  // == Start handlers
  // =========================
  const handleStartFullTest = () => {
    onClose();
    if (typeof onStartFullTest === "function") {
      onStartFullTest(exam, duration);
    }
  };

  const handleStartIndividual = () => {
    const task = Array.isArray(tasks)
      ? tasks.find((t) => String(getTaskId(t)) === String(selectedTaskId))
      : null;
    if (!task) return;

    const duration = getTaskDurationMinutes(task); // ✅ Lấy thời lượng riêng

    onClose();
    if (typeof onStartIndividual === "function") {
      onStartIndividual(exam, task, duration); // ✅ Truyền thêm duration
    }
  };

  // =========================
  // == Render
  // =========================
  return (
    <div
      className={styles.modalOverlay}
      role="dialog"
      aria-modal="true"
      aria-labelledby="examSkillModalTitle"
      tabIndex="-1"
    >
      <div className={styles.modal}>
        <div className={styles.modalHeader}>
          <h3 id="examSkillModalTitle" className={styles.modalTitle}>
            {exam.examName} ({exam.examType})
          </h3>
          <button
            className={styles.iconBtn}
            onClick={onClose}
            aria-label="Close dialog"
            title="Close"
          >
            ×
          </button>
        </div>

        {loading ? (
          <div className={styles.stateText}>
            <Loader2 size={18} className={styles.spin} /> Loading tasks...
          </div>
        ) : (
          <div className={`${styles.modalContent} ${styles.modalGrid}`}>
            {/* ===== Left column: Full test ===== */}
            <div className={styles.modalColumn}>
              <div className={styles.blockCard}>
                <div className={styles.blockHeader}>Do the whole test</div>
                <div className={styles.blockBody}>
                  <div className={styles.infoRow}>
                    <span className={styles.infoLabel}>Instructions</span>
                    <div className={styles.infoValue}>
                      <div className={styles.timeRow} ref={dropdownRef}>
                        <span>
                          Time: <strong>{duration} minutes</strong>
                        </span>
                        <button
                          type="button"
                          className={styles.inlineEditBtn}
                          onClick={() => setIsEditingTime((prev) => !prev)}
                          title="Edit time"
                        >
                          <Edit2 size={16} />
                        </button>

                        {isEditingTime && (
                          <div className={styles.dropdown}>
                            {getDurationOptions(skillType).map((min) => (
                              <div
                                key={min}
                                className={styles.dropdownItem}
                                onClick={() => handleDurationChange(min)}
                              >
                                {min}
                              </div>
                            ))}
                          </div>
                        )}
                      </div>

                      <ul className={styles.list}>
                        <li>Answer all questions</li>
                        <li>You can switch between tasks during the test.</li>
                      </ul>
                    </div>
                  </div>

                  <div className={styles.infoRow}>
                    <span className={styles.infoLabel}>Test info</span>
                    <div className={styles.infoValue}>
                      There are{" "}
                      <span className={styles.highlightNumber}>
                        {totalTasks || "N/A"}
                      </span>{" "}
                      tasks in the test
                      {taskTypes ? ` (${taskTypes})` : ""}.
                    </div>
                  </div>
                </div>

                <div
                  className={`${styles.blockActions} ${styles.centerActions}`}
                >
                  <button
                    className={`${styles.primaryBtn} ${styles.elevatedBtn}`}
                    onClick={handleStartFullTest}
                  >
                    Start Full Test
                  </button>
                </div>
              </div>
            </div>

            {/* ===== Right column: Individual task ===== */}
            <div className={styles.modalColumn}>
              <div className={styles.blockCard}>
                <div className={styles.blockHeader}>Test individual task</div>
                <div className={styles.blockBody}>
                  <div className={styles.taskList}>
                    {Array.isArray(tasks) && tasks.length > 0 ? (
                      tasks
                        .sort((a, b) => getDisplayOrder(a) - getDisplayOrder(b))
                        .map((t) => {
                          const id = getTaskId(t);
                          const selected =
                            String(selectedTaskId) === String(id);
                          return (
                            <label
                              key={id}
                              className={`${styles.taskOption} ${
                                selected ? styles.selectedTask : ""
                              }`}
                            >
                              <input
                                type="radio"
                                name="task"
                                value={id}
                                checked={selected}
                                onChange={() => setSelectedTaskId(id)}
                              />
                              <div className={styles.taskMeta}>
                                <div className={styles.taskTitle}>
                                  {getTaskLabel(t)} — Task {getDisplayOrder(t)}{" "}
                                  ({getTaskDurationMinutes(t)} min)
                                </div>
                              </div>
                            </label>
                          );
                        })
                    ) : (
                      <div className={styles.stateText}>
                        No tasks available.
                      </div>
                    )}
                  </div>
                </div>

                <div
                  className={`${styles.blockActions} ${styles.centerActions}`}
                >
                  <button
                    className={`${styles.primaryBtn} ${styles.elevatedBtn}`}
                    disabled={!selectedTaskId}
                    onClick={handleStartIndividual}
                  >
                    Start Task
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
