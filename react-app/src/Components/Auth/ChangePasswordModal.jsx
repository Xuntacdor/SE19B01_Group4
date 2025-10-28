import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { changePassword } from '../../Services/AuthApi';
import { AlertCircle, CheckCircle, Loader2 } from 'lucide-react';
import AuthLayout from '../Layout/AuthLayout';
import PasswordInputField from './PasswordInputField';
import Button from './Button';
import './ChangePasswordModal.css';

const ChangePasswordModal = ({ isOpen, onClose }) => {
  const [formData, setFormData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
    setError('');
  };

  const handleClose = () => {
    if (!loading) {
      setFormData({
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
      });
      setError('');
      setSuccess(false);
      onClose();
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    setSuccess(false);

    // Validation
    if (formData.newPassword !== formData.confirmPassword) {
      setError('New passwords do not match');
      setLoading(false);
      return;
    }

    if (formData.currentPassword === formData.newPassword) {
      setError('New password must be different from current password');
      setLoading(false);
      return;
    }

    try {
      const response = await changePassword(
        formData.currentPassword,
        formData.newPassword,
        formData.confirmPassword
      );
      setSuccess(true);
      // Close modal after 2 seconds
      setTimeout(() => {
        handleClose();
      }, 2000);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to change password. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="change-password-modal-overlay" onClick={handleClose}>
      <div className="change-password-modal-content" onClick={(e) => e.stopPropagation()}>
        <button 
          className="modal-close-btn" 
          onClick={handleClose}
          disabled={loading}
        >
          ×
        </button>

        <AuthLayout title="Change Password">
          <form onSubmit={handleSubmit} className="login-form">
            <PasswordInputField
              name="currentPassword"
              placeholder="Current Password"
              value={formData.currentPassword}
              onChange={handleChange}
              disabled={loading}
            />

            <PasswordInputField
              name="newPassword"
              placeholder="New Password"
              value={formData.newPassword}
              onChange={handleChange}
              disabled={loading}
            />

            <PasswordInputField
              name="confirmPassword"
              placeholder="Confirm New Password"
              value={formData.confirmPassword}
              onChange={handleChange}
              disabled={loading}
            />

            {error && (
              <div className="error-message">
                <div className="error-icon">
                  <AlertCircle size={16} />
                </div>
                <div className="error-text">{error}</div>
              </div>
            )}

            {success && (
              <div className="success-message">
                <div className="success-icon">
                  <CheckCircle size={16} />
                </div>
                <div className="success-text">Password changed successfully!</div>
              </div>
            )}

            <div className="button-location">
              <Button type="submit" variant="yellow" disabled={loading}>
                {loading ? (
                  <div className="loading-content">
                    <Loader2 size={10} className="loading-spinner" />
                    <span>Changing...</span>
                  </div>
                ) : (
                  'Change Password'
                )}
              </Button>
            </div>

            <div className="button-location" style={{ marginTop: '8px' }}>
              <Button type="button" variant="outline" onClick={handleClose} disabled={loading}>
                Cancel
              </Button>
            </div>
          </form>

        </AuthLayout>
      </div>
    </div>
  );
};

export default ChangePasswordModal;
