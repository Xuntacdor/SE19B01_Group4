import React from "react";
import { Check, X, AlertTriangle, Info } from "lucide-react";
import PopupBase from "../Common/PopupBase";
import "./NotificationPopup.css";

export default function NotificationPopup({ 
  isOpen, 
  onClose, 
  type = "success", 
  title, 
  message, 
  duration = 5000 
}) {
  React.useEffect(() => {
    if (isOpen && duration > 0) {
      const timer = setTimeout(() => {
        onClose();
      }, duration);

      return () => clearTimeout(timer);
    }
  }, [isOpen, duration, onClose]);

  const getIcon = () => {
    switch (type) {
      case "success":
        return Check;
      case "error":
        return X;
      case "warning":
        return AlertTriangle;
      default:
        return Info;
    }
  };

  const getIconClass = () => {
    switch (type) {
      case "success":
        return "notification-icon success";
      case "error":
        return "notification-icon error";
      case "warning":
        return "notification-icon warning";
      default:
        return "notification-icon info";
    }
  };

  const getIconContainerClass = () => {
    switch (type) {
      case "success":
        return "notification-icon-large success-bg";
      case "error":
        return "notification-icon-large error-bg";
      case "warning":
        return "notification-icon-large warning-bg";
      default:
        return "notification-icon-large info-bg";
    }
  };

  const Icon = getIcon();

  return (
    <PopupBase
      hideHeader={true}
      show={isOpen}
      width="400px"
      onClose={onClose}
    >
      <div className="notification-content">
        <div className={getIconContainerClass()}>
          <Icon size={48} className={getIconClass()} strokeWidth={3} />
        </div>
        {title && <h3 className="notification-title">{title}</h3>}
        <p className="notification-message">{message}</p>
      </div>
      
      <div className="notification-footer">
        <button className={`notification-btn ${type === "error" ? "error" : ""}`} onClick={onClose}>
          OK
        </button>
      </div>
    </PopupBase>
  );
}

