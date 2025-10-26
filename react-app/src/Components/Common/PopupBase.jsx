// react-app/src/Components/Common/PopupBase.jsx
import React from "react";
import { X } from "lucide-react";
import "./PopupBase.css";

export default function PopupBase({
  title,
  icon: Icon,
  width = "600px",
  show = false,
  onClose,
  children,
}) {
  if (!show) return null;

  return (
    <div className="popup-overlay" onClick={onClose}>
      <div
        className="popup-container"
        style={{ width }}
        onClick={(e) => e.stopPropagation()} // prevent overlay click close
      >
        <div className="popup-header">
          <div className="popup-title">
            {Icon && <Icon size={20} className="popup-icon" />}
            <h3>{title}</h3>
          </div>
          <button className="popup-close" onClick={onClose}>
            <X size={20} />
          </button>
        </div>

        <div className="popup-body">{children}</div>
      </div>
    </div>
  );
}
