import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { verifyOtp, forgotPassword } from '../../Services/AuthApi';
import Button from '../../Components/Auth/Button';
import { Loader2, ArrowLeft, Clock, AlertTriangle, AlertCircle } from 'lucide-react';
import './Login.css';
import './VerifyOtp.css';

const VerifyOtp = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const [otpCode, setOtpCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [timeLeft, setTimeLeft] = useState(60); // 1 minute in seconds
  const timerRef = useRef(null);

  const email = location.state?.email;

  // Function to start the timer
  const startTimer = useCallback(() => {
    // Clear any existing timer
    if (timerRef.current) {
      clearInterval(timerRef.current);
    }

    timerRef.current = setInterval(() => {
      setTimeLeft((prev) => {
        if (prev <= 1) {
          if (timerRef.current) {
            clearInterval(timerRef.current);
            timerRef.current = null;
          }
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  }, []);

  useEffect(() => {
    if (!email) {
      navigate('/login?mode=forgot');
      return;
    }

    startTimer();

    return () => {
      if (timerRef.current) {
        clearInterval(timerRef.current);
        timerRef.current = null;
      }
    };
  }, [email, navigate, startTimer]);

  const formatTime = (seconds) => {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const response = await verifyOtp(email, otpCode);
      if (response.data.message === 'OTP verified successfully') {
        navigate('/reset-password', { state: { email, resetToken: response.data.resetToken } });
      }
    } catch (err) {
      const errorData = err.response?.data;
      setError(errorData?.message || 'Invalid OTP. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleOtpChange = (e) => {
    const value = e.target.value.replace(/\D/g, '').slice(0, 6);
    setOtpCode(value);
    if (error) setError('');
  };

  const handleResendOtp = async () => {
    setLoading(true);
    setError('');
    try {
      await forgotPassword(email);
      // Clear existing timer
      if (timerRef.current) {
        clearInterval(timerRef.current);
        timerRef.current = null;
      }
      // Reset timer to 1 minute and restart
      setTimeLeft(60);
      startTimer();
      setOtpCode('');
    } catch (err) {
      const errorData = err.response?.data;
      setError(errorData?.message || 'Failed to resend OTP. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  if (!email) {
    return null;
  }

  return (
    <div className="animated-login-page">
      <div className="animated-login-container verify-otp-container">
        <div className="form-container sign-in-container verify-otp-form-container">
          <form onSubmit={handleSubmit} className="animated-form" noValidate>
            <div className="website-branding verify-otp-branding">
              <h2 className="brand-title verify-otp-brand-title">IELTS PHOBIC</h2>
            </div>
            
            <h1>Verify Your Email</h1>
            
            <div className="verify-otp-header">
              <p>We've sent a 6-digit verification code to</p>
              <p className="email-address">{email}</p>
            </div>

            {error && (
              <div className="error-message">
                <div className="error-icon">
                  <AlertCircle size={18} />
                </div>
                <div className="error-text">{error}</div>
              </div>
            )}

            <div className="form-group otp-input-group">
              <input
                type="text"
                id="otp"
                value={otpCode}
                onChange={handleOtpChange}
                placeholder="000000"
                maxLength="6"
                required
                disabled={loading}
                className="verify-otp-input"
              />
            </div>

            {timeLeft > 0 && (
              <div className="timer">
                <Clock size={18} />
                <span>Code expires in {formatTime(timeLeft)}</span>
              </div>
            )}

            {timeLeft === 0 && (
              <div className="expired-message">
                <AlertTriangle size={18} />
                <span>Verification code has expired</span>
              </div>
            )}

            <div className="button-wrapper">
              <Button 
                type="submit" 
                variant="yellow"
                disabled={loading || otpCode.length !== 6 || timeLeft === 0}
              >
                {loading ? (
                  <div className="loading-content">
                    <Loader2 size={16} className="loading-spinner" />
                    <span>Verifying...</span>
                  </div>
                ) : (
                  "Verify Code"
                )}
              </Button>
            </div>

            <div className="resend-section">
              <p>Didn't receive the code?</p>
              <button 
                type="button"
                className="resend-btn"
                onClick={handleResendOtp}
                disabled={loading || timeLeft > 0}
              >
                {loading ? 'Sending...' : 'Resend Code'}
              </button>
            </div>

            <a 
              href="#" 
              className="forgot-link" 
              onClick={(e) => { 
                e.preventDefault(); 
                navigate('/login?mode=forgot'); 
              }}
            >
              ← Back to Email
            </a>
          </form>
        </div>
      </div>
    </div>
  );
};

export default VerifyOtp;
