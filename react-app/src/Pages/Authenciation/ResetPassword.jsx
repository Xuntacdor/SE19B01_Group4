import React, { useState, useEffect, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { createPortal } from 'react-dom';
import { resetPassword } from '../../Services/AuthApi';
import Button from '../../Components/Auth/Button';
import PasswordInputField from '../../Components/Auth/PasswordInputField';
import { Loader2, CheckCircle, AlertCircle, Check, X, XCircle } from 'lucide-react';
import './Login.css';
import './ResetPassword.css';

// Reusable Password Guide Popup Component
const PasswordGuidePopup = ({ show, position, popupRef, requirements, onClose }) => {
  if (!show) return null;
  
  return createPortal(
    <div 
      ref={popupRef}
      className="password-guide-popup"
      style={{
        position: 'fixed',
        top: `${position.top}px`,
        left: `${position.left}px`
      }}
    >
      <button
        className="password-guide-close-btn"
        onClick={onClose}
        type="button"
        aria-label="Close password guide"
      >
        <XCircle size={18} />
      </button>
      <div className="password-guide-title">
        <AlertCircle size={14} />
        Password Requirements
      </div>
      <ul className="password-guide-list">
        <li className={`password-guide-item ${requirements.minLength ? 'valid' : 'invalid'}`}>
          {requirements.minLength ? (
            <Check size={14} className="password-guide-icon" />
          ) : (
            <X size={14} className="password-guide-icon" />
          )}
          <span>At least 6 characters</span>
        </li>
      </ul>
    </div>,
    document.body
  );
};

const ResetPassword = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    newPassword: '',
    confirmPassword: ''
  });
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [showPasswordGuide, setShowPasswordGuide] = useState(false);
  const [popupPosition, setPopupPosition] = useState({ top: 0, left: 0 });
  const passwordInputRef = useRef(null);
  const guidePopupRef = useRef(null);

  const { email, resetToken } = location.state || {};

  useEffect(() => {
    if (!email || !resetToken) {
      navigate('/login?mode=forgot');
      return;
    }
  }, [email, resetToken, navigate]);

  // Update popup position when it's shown
  useEffect(() => {
    if (showPasswordGuide && passwordInputRef.current) {
      const updatePosition = () => {
        const rect = passwordInputRef.current.getBoundingClientRect();
        const popupWidth = 300; // Approximate popup width
        const gap = 12;
        let leftPosition = rect.left - popupWidth - gap;
        
        // Prevent going off-screen on the left
        if (leftPosition < 10) {
          leftPosition = 10;
        }
        
        setPopupPosition({
          top: rect.top,
          left: leftPosition
        });
      };
      
      updatePosition();
      window.addEventListener('scroll', updatePosition, true);
      window.addEventListener('resize', updatePosition);
      
      return () => {
        window.removeEventListener('scroll', updatePosition, true);
        window.removeEventListener('resize', updatePosition);
      };
    }
  }, [showPasswordGuide]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
    
    // Show guide when user starts typing in password field
    if (name === 'newPassword' && value.length > 0) {
      setShowPasswordGuide(true);
    }
  };

  const handlePasswordFocus = () => {
    setShowPasswordGuide(true);
  };

  const handlePasswordBlur = (e) => {
    // Delay hiding to allow clicking on the guide
    setTimeout(() => {
      if (!guidePopupRef.current?.contains(document.activeElement)) {
        setShowPasswordGuide(false);
      }
    }, 200);
  };

  // Check password requirements
  const checkPasswordRequirements = (password) => {
    return {
      minLength: password.length >= 6
    };
  };

  const passwordRequirements = checkPasswordRequirements(formData.newPassword);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    setMessage('');

    if (formData.newPassword !== formData.confirmPassword) {
      setError('Passwords do not match');
      setLoading(false);
      return;
    }

    if (formData.newPassword.length < 6) {
      setError('Password must be at least 6 characters long');
      setLoading(false);
      return;
    }

    try {
      const response = await resetPassword(
        email, 
        resetToken, 
        formData.newPassword, 
        formData.confirmPassword
      );
      setMessage(response.data.message);
      
      // Redirect to login after 3 seconds
      setTimeout(() => {
        navigate('/login');
      }, 3000);
    } catch (err) {
      setError(err.response?.data?.message || 'An error occurred. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  if (!email || !resetToken) {
    return null;
  }

  return (
    <div className="animated-login-page">
      <div className="animated-login-container reset-password-container">
        <div className="form-container sign-in-container reset-password-form-container">
          <form onSubmit={handleSubmit} className="animated-form" noValidate>
            <div className="website-branding reset-password-branding">
              <h2 className="brand-title reset-password-brand-title">IELTS PHOBIC</h2>
            </div>
            
            <h1>Reset Your Password</h1>
            
            <div className="reset-password-header">
              <p>Enter your new password below</p>
            </div>

            {error && (
              <div className="error-message">
                <div className="error-icon">
                  <AlertCircle size={18} />
                </div>
                <div className="error-text">{error}</div>
              </div>
            )}

            {message && (
              <div className="success-message">
                <div className="success-icon">
                  <CheckCircle size={18} />
                </div>
                <div className="success-text">
                  <div>{message}</div>
                </div>
              </div>
            )}

            <div className="form-group reset-password-input-group" style={{ position: 'relative' }}>
              <PasswordInputField
                name="newPassword"
                placeholder="Enter new password"
                value={formData.newPassword}
                onChange={handleInputChange}
                onFocus={handlePasswordFocus}
                onBlur={handlePasswordBlur}
                disabled={loading}
                minLength={6}
                title="Password must be at least 6 characters"
                inputRef={passwordInputRef}
              />
              <PasswordGuidePopup
                show={showPasswordGuide}
                position={popupPosition}
                popupRef={guidePopupRef}
                requirements={passwordRequirements}
                onClose={() => setShowPasswordGuide(false)}
              />
            </div>

            <div className="form-group reset-password-input-group">
              <PasswordInputField
                name="confirmPassword"
                placeholder="Confirm new password"
                value={formData.confirmPassword}
                onChange={handleInputChange}
                disabled={loading}
                minLength={6}
                title="Please confirm your password"
              />
              {formData.confirmPassword && formData.newPassword !== formData.confirmPassword && (
                <div className="password-mismatch">
                  <AlertCircle size={16} />
                  <span>Passwords do not match</span>
                </div>
              )}
            </div>

            <div className="button-wrapper">
              <Button 
                type="submit" 
                variant="yellow"
                disabled={loading || formData.newPassword !== formData.confirmPassword || formData.newPassword.length < 6}
              >
                {loading ? (
                  <div className="loading-content">
                    <Loader2 size={16} className="loading-spinner" />
                    <span>Resetting Password...</span>
                  </div>
                ) : (
                  "Reset Password"
                )}
              </Button>
            </div>

            <a 
              href="#" 
              className="forgot-link" 
              onClick={(e) => { 
                e.preventDefault(); 
                navigate('/verify-otp', { state: { email } }); 
              }}
            >
              ← Back to Verification
            </a>
          </form>
        </div>
      </div>
    </div>
  );
};

export default ResetPassword;
