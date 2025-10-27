import React from "react";
import { BookOpen } from "lucide-react";
import PopupBase from "../Common/PopupBase";
import styles from "./PopUp.module.css";

export default function Popup({ title, onClose, children, actions, show = true }) {
  if (!show) return null;
  
  return (
    <PopupBase
      title={title}
      icon={BookOpen}
      show={true}
      width="520px"
      onClose={onClose}
    >
      <div className={styles.content}>{children}</div>
      {actions && <div className={styles.actions}>{actions}</div>}
    </PopupBase>
  );
}
