import React, { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { resetPassword } from '../../Services/AuthApi';
import Button from '../../Components/Auth/Button';
import PasswordInputField from '../../Components/Auth/PasswordInputField';
import { Loader2, ArrowLeft, CheckCircle, AlertCircle } from 'lucide-react';
import './Login.css';
import './ResetPassword.css';

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

  const { email, resetToken } = location.state || {};

  useEffect(() => {
    if (!email || !resetToken) {
      navigate('/login?mode=forgot');
      return;
    }
  }, [email, resetToken, navigate]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

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
                  <AlertCircle size={14} />
                </div>
                <div className="error-text">{error}</div>
              </div>
            )}

            {message && (
              <div className="success-message">
                <div className="success-icon">
                  <CheckCircle size={14} />
                </div>
                <div className="success-text">
                  {message}
                  <p style={{ marginTop: '4px', fontSize: '10px', opacity: 0.8 }}>Redirecting to login page...</p>
                </div>
              </div>
            )}

            <div className="form-group reset-password-input-group">
              <PasswordInputField
                name="newPassword"
                placeholder="Enter new password"
                value={formData.newPassword}
                onChange={handleInputChange}
                disabled={loading}
                minLength={6}
                title="Password must be at least 6 characters"
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
                  <AlertCircle size={12} />
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
                    <Loader2 size={12} className="loading-spinner" />
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
