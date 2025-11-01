import PopupBase from "../Common/PopupBase";
import React, {
  useState,
  useEffect,
  useRef,
  useMemo,
  useCallback,
} from "react";
import { ClipboardList, Edit2, Loader2 } from "lucide-react";
import styles from "./ExamPopup.module.css";

export default function ExamSkillModal({
  show = true,
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

  // ============ Duration logic ============
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

  useEffect(() => {
    const handleClickOutside = (e) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target))
        setIsEditingTime(false);
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

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

  const getDisplayOrder = (t) => t.displayOrder ?? t.DisplayOrder ?? 0;
  const getTaskId = (t) =>
    t.readingId || t.listeningId || t.writingId || t.speakingId;
  const getTaskLabel = (t) =>
    t.readingType ||
    t.listeningType ||
    t.writingType ||
    t.speakingType ||
    "Task";

  const getTaskDurationMinutes = (t) => {
    const order = Number(getDisplayOrder(t));
    if (skillType === "Speaking") {
      if (order === 1) return 5;
      if (order === 2) return 3;
      if (order === 3) return 5;
      return 4;
    }
    if (order === 1) return 20;
    if (order === 2) return 40;
    return 30;
  };

  const handleStartFullTest = () => {
    onClose();
    onStartFullTest?.(exam, duration);
  };

  const handleStartIndividual = () => {
    if (skillType === "Speaking") {
      const grouped = tasks.reduce((acc, t) => {
        const type = t.speakingType || "Unknown";
        const baseOrder = Math.floor(
          (t.displayOrder ?? t.DisplayOrder ?? 0) / 10
        );
        const key = `${type}-Group${baseOrder}`;
        if (!acc[key]) acc[key] = [];
        acc[key].push(t);
        return acc;
      }, {});

      const selectedTasks = grouped[selectedTaskId] || [];
      if (selectedTasks.length === 0) return;

      const duration = selectedTasks.length * 3;
      onClose();
      onStartIndividual?.(exam, null, duration, selectedTasks);
      return;
    }

    const task = tasks?.find(
      (t) => String(getTaskId(t)) === String(selectedTaskId)
    );
    if (!task) return;
    const taskDuration = getTaskDurationMinutes(task);
    onClose();
    onStartIndividual?.(exam, task, taskDuration);
  };

  // ======== Group Speaking tasks by Part ========
  const groupedSpeakingTasks = useMemo(() => {
    if (!Array.isArray(tasks)) return {};

    return tasks.reduce((acc, t) => {
      const type = t.speakingType || "Unknown";
      const baseOrder = Math.floor(
        (t.displayOrder ?? t.DisplayOrder ?? 0) / 10
      );
      const key = `${type}-Group${baseOrder}`;
      if (!acc[key]) acc[key] = [];
      acc[key].push(t);
      return acc;
    }, {});
  }, [tasks]);

  return (
    <PopupBase
      title={`${exam?.examName} (${exam?.examType})`}
      icon={ClipboardList}
      show={show}
      width="760px"
      onClose={onClose}
    >
      {loading ? (
        <div className={styles.stateText}>
          <Loader2 size={18} className={styles.spin} /> Loading tasks...
        </div>
      ) : (
        <div className={`${styles.modalContent} ${styles.modalGrid}`}>
          {/* ===== Full test ===== */}
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
                        onClick={() => setIsEditingTime((p) => !p)}
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
                    tasks {taskTypes ? `(${taskTypes})` : ""}.
                  </div>
                </div>
              </div>

              <div className={styles.blockActions}>
                <button
                  className={`${styles.primaryBtn} ${styles.elevatedBtn}`}
                  onClick={handleStartFullTest}
                >
                  Start Full Test
                </button>
              </div>
            </div>
          </div>

          {/* ===== Individual task ===== */}
          <div className={styles.modalColumn}>
            <div className={styles.blockCard}>
              <div className={styles.blockHeader}>Test individual task</div>
              <div className={styles.blockBody}>
                <div className={styles.taskList}>
                  {Array.isArray(tasks) && tasks.length > 0 ? (
                    skillType === "Speaking" ? (
                      // Hiển thị nhóm theo Part và sắp xếp Part1→Part2→Part3
                      Object.entries(groupedSpeakingTasks)
                        .sort(([aKey], [bKey]) => {
                          const aPart = aKey.match(/Part\s?(\d+)/)?.[1];
                          const bPart = bKey.match(/Part\s?(\d+)/)?.[1];
                          return (
                            (parseInt(aPart) || 99) - (parseInt(bPart) || 99)
                          );
                        })
                        .map(([groupKey, list]) => {
                          const [part] = groupKey.split("-");
                          return (
                            <label
                              key={groupKey}
                              className={`${styles.taskOption} ${
                                selectedTaskId === groupKey
                                  ? styles.selectedTask
                                  : ""
                              }`}
                            >
                              <input
                                type="radio"
                                name="task"
                                value={groupKey}
                                checked={selectedTaskId === groupKey}
                                onChange={() => setSelectedTaskId(groupKey)}
                              />
                              <div className={styles.taskMeta}>
                                <div className={styles.taskTitle}>
                                  {part} ({list.length} câu)
                                </div>
                              </div>
                            </label>
                          );
                        })
                    ) : (
                      // Giữ nguyên logic cũ cho các kỹ năng khác
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
                    )
                  ) : (
                    <div className={styles.stateText}>No tasks available.</div>
                  )}
                </div>
              </div>

              <div className={styles.blockActions}>
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
    </PopupBase>
  );
}
