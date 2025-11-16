import React from "react";
import PopupBase from "./PopupBase";
import { Trash2 } from "lucide-react";
import styles from "./DeleteConfirmPopup.module.css";

export default function DeleteConfirmPopup({ show, title, message, onCancel, onConfirm }) {
  return (
    <PopupBase
      show={show}
      width="420px"
      title={title}
      icon={Trash2}
      onClose={onCancel}
    >
      <div className={styles.body}>
        <p>{message}</p>

        <div className={styles.actions}>
          <button className={styles.cancelBtn} onClick={onCancel}>
            Cancel
          </button>

          <button className={styles.deleteBtn} onClick={onConfirm}>
            <Trash2 size={16} style={{ marginRight: 6 }} />
            Delete
          </button>
        </div>
      </div>
    </PopupBase>
  );
}
