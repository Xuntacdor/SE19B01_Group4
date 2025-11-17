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

  const iconConfig = {
    success: {
      Icon: Check,
      color: "#10b981",
      bgColor: "#d1fae5"
    },
    error: {
      Icon: X,
      color: "#ef4444",
      bgColor: "#fee2e2"
    },
    warning: {
      Icon: AlertTriangle,
      color: "#f59e0b",
      bgColor: "#fef3c7"
    },
    info: {
      Icon: Info,
      color: "#3b82f6",
      bgColor: "#dbeafe"
    }
  };

  const config = iconConfig[type] || iconConfig.info;
  const { Icon, color, bgColor } = config;

  return (
    <PopupBase
      hideHeader={true}
      show={isOpen}
      width="400px"
      onClose={onClose}
    >
      <div className="notification-wrapper">
        <div className="notification-content">
          <div 
            className="notification-icon-container"
            style={{ backgroundColor: bgColor }}
          >
            <Icon 
              size={48} 
              strokeWidth={3}
              color={color}
              style={{ 
                display: "block",
                width: "48px",
                height: "48px"
              }}
            />
          </div>
          {title && <h3 className="notification-title">{title}</h3>}
          <p className="notification-message">{message}</p>
        </div>
        
        <div className="notification-footer">
          <button 
            className="notification-btn" 
            onClick={onClose}
          >
            OK
          </button>
        </div>
      </div>
    </PopupBase>
  );
}
