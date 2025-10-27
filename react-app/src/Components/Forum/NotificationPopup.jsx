import React from "react";
import { CheckCircle, XCircle, AlertCircle, Info } from "lucide-react";
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
        return CheckCircle;
      case "error":
        return XCircle;
      case "warning":
        return AlertCircle;
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

  const Icon = getIcon();

  return (
    <div className="notification-popup">
      <PopupBase
        hideHeader={true}
        show={isOpen}
        width="400px"
        onClose={onClose}
      >
        <div className="notification-content">
          <div className="notification-icon-large">
            <Icon size={32} className={getIconClass()} />
          </div>
          {title && <h3 className="notification-title">{title}</h3>}
          <p className="notification-message">{message}</p>
        </div>
        
        <div className="notification-footer">
          <button className="notification-btn" onClick={onClose}>
            OK
          </button>
        </div>
      </PopupBase>
    </div>
  );
}

