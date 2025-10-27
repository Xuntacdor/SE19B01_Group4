import React, { useState } from "react";
import { AlertCircle } from "lucide-react";
import PopupBase from "./PopupBase";
import "./RejectionReasonPopup.css";

export default function RejectionReasonPopup({
  isOpen,
  onClose,
  onConfirm,
  title = "Reject Post",
}) {
  const [reason, setReason] = useState("");
  const [error, setError] = useState("");

  const handleSubmit = (e) => {
    e.preventDefault();
    
    if (!reason.trim()) {
      setError("Please enter a rejection reason");
      return;
    }

    onConfirm?.(reason);
    setReason("");
    setError("");
    onClose();
  };

  const handleClose = () => {
    setReason("");
    setError("");
    onClose();
  };

  return (
    <PopupBase
      title={title}
      icon={AlertCircle}
      show={isOpen}
      width="500px"
      onClose={handleClose}
    >
      <form className="rejection-reason-popup-content" onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="rejection-reason">Enter rejection reason:</label>
          <textarea
            id="rejection-reason"
            className="rejection-input"
            value={reason}
            onChange={(e) => {
              setReason(e.target.value);
              setError("");
            }}
            placeholder="Please explain why this post is being rejected..."
            rows={5}
            required
          />
          {error && <div className="error-message">{error}</div>}
        </div>

        <div className="rejection-actions">
          <button type="button" className="btn-cancel" onClick={handleClose}>
            Cancel
          </button>
          <button type="submit" className="btn-confirm danger">
            Reject Post
          </button>
        </div>
      </form>
    </PopupBase>
  );
}


