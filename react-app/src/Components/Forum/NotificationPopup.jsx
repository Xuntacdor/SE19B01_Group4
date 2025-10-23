import React from "react";
import { CheckCircle, XCircle, AlertCircle, X } from "lucide-react";
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

  if (!isOpen) return null;

  const getIcon = () => {
    switch (type) {
      case "success":
        return <CheckCircle size={24} className="notification-icon success" />;
      case "error":
        return <XCircle size={24} className="notification-icon error" />;
      case "warning":
        return <AlertCircle size={24} className="notification-icon warning" />;
      default:
        return <CheckCircle size={24} className="notification-icon success" />;
    }
  };

  return (
    <div className="notification-overlay">
      <div className="notification-popup">
        <div className="notification-header">
          <div className="notification-icon-container">
            {getIcon()}
          </div>
          <button className="notification-close" onClick={onClose}>
            <X size={20} />
          </button>
        </div>
        
        <div className="notification-content">
          {title && <h3 className="notification-title">{title}</h3>}
          <p className="notification-message">{message}</p>
        </div>
        
        <div className="notification-footer">
          <button className="notification-btn" onClick={onClose}>
            OK
          </button>
        </div>
      </div>
    </div>
  );
}
