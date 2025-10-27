import React from "react";
import { AlertTriangle } from "lucide-react";
import PopupBase from "./PopupBase";
import "./ConfirmationPopup.css";

export default function ConfirmationPopup({
  isOpen,
  onClose,
  onConfirm,
  title = "Confirmation",
  message,
  confirmText = "OK",
  cancelText = "Cancel",
  type = "warning",
}) {
  const handleConfirm = () => {
    onConfirm?.();
    onClose();
  };

  const getIcon = () => {
    switch (type) {
      case "danger":
      case "warning":
        return AlertTriangle;
      default:
        return AlertTriangle;
    }
  };

  const Icon = getIcon();

  return (
    <PopupBase
      title={title}
      icon={Icon}
      show={isOpen}
      width="400px"
      onClose={onClose}
    >
      <div className="confirmation-popup-content">
        <p className="confirmation-message">{message}</p>
        
        <div className="confirmation-actions">
          <button className="btn-cancel" onClick={onClose}>
            {cancelText}
          </button>
          <button className={`btn-confirm ${type === "danger" ? "danger" : ""}`} onClick={handleConfirm}>
            {confirmText}
          </button>
        </div>
      </div>
    </PopupBase>
  );
}

