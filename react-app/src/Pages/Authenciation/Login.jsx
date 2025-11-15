import React, { useState, useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import FormInput from "../../Components/Auth/InputField";
import PasswordInputField from "../../Components/Auth/PasswordInputField";
import Button from "../../Components/Auth/Button";
import google from "../../assets/google.png";
import "./Login.css";
import { login, register, forgotPassword } from "../../Services/AuthApi";

import { useNavigate } from "react-router-dom";
import { 
  User, 
  Lock,
  Mail, 
  AlertCircle, 
  CheckCircle, 
  Loader2,
  Check,
  X,
  XCircle
} from "lucide-react";


// Reusable Message Components
const ErrorMessage = ({ message, icon }) => (
  <div className="error-message">
    <div className="error-icon">{icon}</div>
    <div className="error-text">{message}</div>
  </div>
);

const SuccessMessage = ({ message }) => (
  <div className="success-message">
    <div className="success-icon"><CheckCircle size={16} /></div>
    <div className="success-text">{message}</div>
  </div>
);

// Reusable Loading Button Content
const LoadingButtonContent = ({ text, iconSize = 14 }) => (
  <div className="loading-content">
    <Loader2 size={iconSize} className="loading-spinner" />
    <span>{text}</span>
  </div>
);

// Reusable Email Guide Popup Component
const EmailGuidePopup = ({ show, position, popupRef, requirements, onClose }) => {
  if (!show) return null;
  
  return createPortal(
    <div 
      ref={popupRef}
      className="email-guide-popup"
      style={{
        position: 'fixed',
        top: `${position.top}px`,
        left: `${position.left}px`
      }}
    >
      <button
        className="email-guide-close-btn"
        onClick={onClose}
        type="button"
        aria-label="Close email guide"
      >
        <XCircle size={18} />
      </button>
      <div className="email-guide-title">
        <AlertCircle size={14} />
        Email Requirements
      </div>
      <ul className="email-guide-list">
        <li className={`email-guide-item ${requirements.hasAtSymbol ? 'valid' : 'invalid'}`}>
          {requirements.hasAtSymbol ? (
            <Check size={14} className="email-guide-icon" />
          ) : (
            <X size={14} className="email-guide-icon" />
          )}
          <span>Must contain @ symbol</span>
        </li>
        <li className={`email-guide-item ${requirements.hasDomain ? 'valid' : 'invalid'}`}>
          {requirements.hasDomain ? (
            <Check size={14} className="email-guide-icon" />
          ) : (
            <X size={14} className="email-guide-icon" />
          )}
          <span>Must have domain (e.g., gmail.com)</span>
        </li>
        <li className={`email-guide-item ${requirements.hasValidFormat ? 'valid' : 'invalid'}`}>
          {requirements.hasValidFormat ? (
            <Check size={14} className="email-guide-icon" />
          ) : (
            <X size={14} className="email-guide-icon" />
          )}
          <span>Valid email format</span>
        </li>
      </ul>
      <div className="email-example">
        Example: user@gmail.com
      </div>
    </div>,
    document.body
  );
};

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

const Login = () => {
    const [mode, setMode] = useState("login");
    const [form, setForm] = useState({ email: "", password: "", username: "", confirmPassword: "" });
    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");
    const [isLoading, setIsLoading] = useState(false);
    const [invalidFields, setInvalidFields] = useState(new Set());
    const [showPasswordGuide, setShowPasswordGuide] = useState(false);
    const [showEmailGuide, setShowEmailGuide] = useState(false);
    const [popupPosition, setPopupPosition] = useState({ top: 0, left: 0 });
    const [emailPopupPosition, setEmailPopupPosition] = useState({ top: 0, left: 0 });
    const passwordInputRef = useRef(null);
    const emailInputRef = useRef(null);
    const guidePopupRef = useRef(null);
    const emailGuidePopupRef = useRef(null);
    const navigate = useNavigate();

    // Handle URL parameters and sync container class
    useEffect(() => {
      const params = new URLSearchParams(window.location.search);
      const modeParam = params.get("mode");
      if (modeParam && modeParam !== mode) {
        setMode(modeParam);
      // Clear all popups when mode changes via URL
      setShowPasswordGuide(false);
      setShowEmailGuide(false);
      setInvalidFields(new Set());
      }

      // Handle OAuth login success
      const loginSuccess = params.get("login");
      if (loginSuccess === "success" && params.get("email")) {
        const user = { 
          email: params.get("email"), 
          username: params.get("username"), 
          role: params.get("role") || "user" 
        };
        localStorage.setItem("user", JSON.stringify(user));
        const routes = { admin: "/admin/users", moderator: "/moderator/dashboard" };
        navigate(routes[user.role] || "/home");
      }
    }, [navigate, mode]);

    // Sync container class with mode changes and clear popups
    useEffect(() => {
      const container = document.getElementById("animated-login-container");
      if (container) {
        container.classList.toggle("right-panel-active", mode === "register");
      }
      // Clear all popups when mode changes
      setShowPasswordGuide(false);
      setShowEmailGuide(false);
      setInvalidFields(new Set());
    }, [mode]);

    // Helper functions
    const getErrorIcon = (errorMessage) => {
      if (errorMessage.includes("Account not found")) return <User size={16} />;
      if (errorMessage.includes("Incorrect password")) return <Lock size={16} />;
      if (errorMessage.includes("Email has already been used")) return <Mail size={16} />;
      return <AlertCircle size={16} />;
    };

    const validateEmail = (email) => {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      return emailRegex.test(email);
    };

    const getEmailValidationError = (email) => {
      if (!email) return "Email is required";
      if (!validateEmail(email)) {
        return "Email is required";
      }
      return null;
    };

    // Consolidated validation logic
    const validateField = (fieldName, value) => {
      if (fieldName === "email") {
        return getEmailValidationError(value);
      }
      
      if (mode === "register") {
        if (fieldName === "username") {
          if (!value) return "Username is required";
          if (value.length < 3) return "Username must be at least 3 characters";
        } else if (fieldName === "password") {
          if (!value) return "Password is required";
          if (value.length < 6) return "Password must be at least 6 characters";
        } else if (fieldName === "confirmPassword") {
          if (!value) return "Please confirm your password";
          if (form.password !== value) return "Passwords do not match";
        }
      } else if (mode === "login" && fieldName === "password") {
        if (!value) return "Password is required";
      }
      
      return null;
    };

    const validateForm = () => {
      const errors = {};
      const fieldsToValidate = mode === "register" 
        ? ["email", "username", "password", "confirmPassword"]
        : mode === "login" 
        ? ["email", "password"]
        : ["email"];
      
      fieldsToValidate.forEach(fieldName => {
        const error = validateField(fieldName, form[fieldName]);
        if (error) errors[fieldName] = error;
      });
      
      return errors;
    };

    // Helper to mark field as invalid
    const markFieldInvalid = (fieldName) => {
      setInvalidFields(prev => new Set(prev).add(fieldName));
    };

    // Helper to mark field as valid
    const markFieldValid = (fieldName) => {
      setInvalidFields(prev => {
        const newSet = new Set(prev);
        newSet.delete(fieldName);
        return newSet;
      });
    };

    const handleChange = (e) => {
      const fieldName = e.target.name;
      const fieldValue = e.target.value;
      
      setForm(prev => ({ ...prev, [fieldName]: fieldValue }));
      if (error) setError("");
      if (success) setSuccess("");
      
      
      // Re-validate confirmPassword when password changes
      if (fieldName === "password" && form.confirmPassword) {
        clearTimeout(window.validationTimeout_confirmPassword);
        window.validationTimeout_confirmPassword = setTimeout(() => {
          const errorMsg = fieldValue !== form.confirmPassword ? "Passwords do not match" : null;
          errorMsg ? markFieldInvalid("confirmPassword") : markFieldValid("confirmPassword");
        }, 500);
      }
      
      // Show password guide when user starts typing in password field (register mode only)
      if (fieldName === 'password' && mode === "register" && fieldValue.length > 0) {
        setShowPasswordGuide(true);
      }

      // Show email guide when user starts typing in email field
      if (fieldName === 'email' && fieldValue.length > 0) {
        setShowEmailGuide(true);
      }

      // Real-time validation with debounce
      clearTimeout(window[`validationTimeout_${fieldName}`]);
      window[`validationTimeout_${fieldName}`] = setTimeout(() => {
        const errorMsg = validateField(fieldName, fieldValue);
        errorMsg ? markFieldInvalid(fieldName) : markFieldValid(fieldName);
      }, 500);
    };

    const handleBlur = (e) => {
      const fieldName = e.target.name;
      const errorMsg = validateField(fieldName, e.target.value);
      errorMsg ? markFieldInvalid(fieldName) : markFieldValid(fieldName);
    };

    const switchMode = (newMode) => {
      setMode(newMode);
      setError("");
      setSuccess("");
      setForm({ email: "", password: "", username: "", confirmPassword: "" });
      setInvalidFields(new Set());
      setShowPasswordGuide(false);
      setShowEmailGuide(false);
    };

    // Check password requirements
    const checkPasswordRequirements = (password) => {
      return {
        minLength: password.length >= 6
      };
    };

    const passwordRequirements = checkPasswordRequirements(form.password);

    useEffect(() => {
      const allPasswordRequirementsMet = passwordRequirements.minLength;
    
      if (allPasswordRequirementsMet && showPasswordGuide) {
        const timer = setTimeout(() => setShowPasswordGuide(false), 500);
        return () => clearTimeout(timer);
      }
    }, [passwordRequirements, showPasswordGuide]);

    const handlePasswordFocus = () => {
      if (mode === "register") {
        setShowPasswordGuide(true);
      }
    };

    const handlePasswordBlur = (e) => {
      // Delay hiding to allow clicking on the guide
      setTimeout(() => {
        if (!guidePopupRef.current?.contains(document.activeElement)) {
          setShowPasswordGuide(false);
        }
      }, 200);
    };

    const handleEmailFocus = () => {
      setShowEmailGuide(true);
    };

    const handleEmailBlur = (e) => {
      // Delay hiding to allow clicking on the guide
      setTimeout(() => {
        if (!emailGuidePopupRef.current?.contains(document.activeElement)) {
          setShowEmailGuide(false);
        }
      }, 200);
    };

    // Check email requirements
    const checkEmailRequirements = (email) => {
      if (!email) {
        return {
          hasAtSymbol: false,
          hasDomain: false,
          hasValidFormat: false
        };
      }
      
      const hasAt = email.includes('@');
      const parts = email.split('@');
      const hasDomain = hasAt && parts.length === 2 && parts[1].includes('.');
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      const hasValidFormat = emailRegex.test(email);
      
      return {
        hasAtSymbol: hasAt,
        hasDomain: hasDomain,
        hasValidFormat: hasValidFormat
      };
    };

    const emailRequirements = checkEmailRequirements(form.email);

    useEffect(() => {
      const allEmailRequirementsMet =
        emailRequirements.hasAtSymbol &&
        emailRequirements.hasDomain &&
        emailRequirements.hasValidFormat;
    
      if (allEmailRequirementsMet && showEmailGuide) {
        const timer = setTimeout(() => setShowEmailGuide(false), 500); // close after short delay
        return () => clearTimeout(timer);
      }
    }, [emailRequirements, showEmailGuide]);

    // Update popup position when it's shown
    useEffect(() => {
      if (showPasswordGuide && passwordInputRef.current && mode === "register") {
        const updatePosition = () => {
          const rect = passwordInputRef.current.getBoundingClientRect();
          setPopupPosition({
            top: rect.bottom + 8,
            left: rect.left
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
    }, [showPasswordGuide, mode]);

    // Update email popup position when it's shown
    useEffect(() => {
      if (showEmailGuide && emailInputRef.current) {
        const updatePosition = () => {
          const rect = emailInputRef.current.getBoundingClientRect();
          setEmailPopupPosition({
            top: rect.bottom + 8,
            left: rect.left
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
    }, [showEmailGuide]);

    // Helper to find input element
    const findInputElement = (formElement, fieldName) => {
      return formElement.querySelector(`input[name="${fieldName}"]`) ||
             formElement.closest('.form-container')?.querySelector(`input[name="${fieldName}"]`) ||
             document.querySelector(`input[name="${fieldName}"]`);
    };

    // Helper to handle API errors
    const handleApiError = (err, defaultMessage) => {
      const statusCode = err.response?.status;
      const errorData = err.response?.data;
      
      // Handle specific error cases
      if (statusCode === 409) {
        // Email already exists
        setError("This email has already been used.");
      } else if (errorData?.message) {
        setError(errorData.message);
      } else if (err.message) {
        setError(err.message);
      } else {
        setError(defaultMessage);
      }
      
      // Safely log error without circular references
      try {
        const errorInfo = {
          statusCode: statusCode,
          message: errorData?.message || err.message || defaultMessage,
          error: errorData ? JSON.parse(JSON.stringify(errorData)) : null
        };
        console.error("Auth error:", errorInfo);
      } catch (e) {
        // If JSON.stringify fails, log only safe properties
        console.error("Auth error:", {
          statusCode: statusCode,
          message: errorData?.message || err.message || defaultMessage
        });
      }
    };

    const handleSubmit = (e) => {
      e.preventDefault();
      const formElement = e.target;
      
      setError("");
      setSuccess("");
      setIsLoading(true);

      const errors = validateForm();
      
      if (Object.keys(errors).length > 0) {
        setIsLoading(false);
        
        // Mark all invalid fields
        Object.keys(errors).forEach(fieldName => {
          markFieldInvalid(fieldName);
        });
        
        // Focus on first error field
        const firstErrorField = Object.keys(errors)[0];
        const inputElement = findInputElement(formElement, firstErrorField);
        if (inputElement) {
          requestAnimationFrame(() => {
            setTimeout(() => {
              inputElement.focus();
            }, 100);
          });
        }
        return;
      }

      // Handle form submission based on mode
      const submitHandlers = {
        login: () => login({ email: form.email, password: form.password })
          .then((res) => {
            const user = res.data;
            localStorage.setItem("user", JSON.stringify(user));
            const routes = {
              admin: "/admin/users",
              moderator: "/moderator/dashboard"
            };
            navigate(routes[user.role] || "/home");
          })
          .catch((err) => handleApiError(err, "Login failed. Please try again."))
          .finally(() => setIsLoading(false)),
        
        register: () => register({ username: form.username, email: form.email, password: form.password })
          .then(() => {
            setSuccess("Account created successfully! Please login with your credentials.");
            setForm({ email: "", password: "", username: "", confirmPassword: "" });
          })
          .catch((err) => handleApiError(err, "Registration failed. Please try again."))
          .finally(() => setIsLoading(false)),
        
        forgot: () => forgotPassword(form.email)
          .then(() => {
            setSuccess("OTP sent successfully to your email address");
            setTimeout(() => navigate("/verify-otp", { state: { email: form.email } }), 1500);
          })
          .catch((err) => handleApiError(err, "Failed to send OTP. Please try again."))
          .finally(() => setIsLoading(false))
      };

      submitHandlers[mode]?.();
    };

    const containerClass = `animated-login-container ${mode === "register" ? "right-panel-active" : ""}`;

    return (
      <div className="animated-login-page">
        <div className={containerClass} id="animated-login-container">
          {/* Sign Up Form */}
          <div className="form-container sign-up-container">
            <form onSubmit={handleSubmit} className="animated-form" noValidate>
              <h1>Create Account</h1>
              <div className="social-container">
                <button
                  type="button"
                  className="social google-btn"
                >
                  <img src={google} alt="Google" className="social-img" />
                </button>
              </div>
              <span>or use your email for registration</span>
              
              {error && mode === "register" && <ErrorMessage message={error} icon={getErrorIcon(error)} />}
              {success && mode === "register" && <SuccessMessage message={success} />}

              <div className={invalidFields.has("username") ? "input-field-error" : ""}>
                <FormInput
                  name="username"
                  type="text"
                  placeholder="Username"
                  value={form.username}
                  onChange={handleChange}
                  onBlur={handleBlur}
                  minLength={3}
                  title="Username must be at least 3 characters"
                />
              </div>

              <div className={invalidFields.has("email") ? "input-field-error" : ""} style={{ position: 'relative' }}>
                <FormInput
                  name="email"
                  type="email"
                  placeholder="Email"
                  value={form.email}
                  onChange={handleChange}
                  onBlur={handleBlur}
                  onFocus={handleEmailFocus}
                  title="Please enter a valid email address"
                  inputRef={emailInputRef}
                />
                <EmailGuidePopup
                  show={showEmailGuide}
                  position={emailPopupPosition}
                  popupRef={emailGuidePopupRef}
                  requirements={emailRequirements}
                  onClose={() => setShowEmailGuide(false)}
                />
              </div>

              <div className={invalidFields.has("password") ? "input-field-error" : ""} style={{ position: 'relative' }}>
                <PasswordInputField
                  name="password"
                  placeholder="Password"
                  value={form.password}
                  onChange={handleChange}
                  onBlur={handlePasswordBlur}
                  onFocus={handlePasswordFocus}
                  minLength={6}
                  title="Password is required and must be at least 6 characters"
                  inputRef={passwordInputRef}
                />
                <PasswordGuidePopup
                  show={showPasswordGuide && mode === "register"}
                  position={popupPosition}
                  popupRef={guidePopupRef}
                  requirements={passwordRequirements}
                  onClose={() => setShowPasswordGuide(false)}
                />
              </div>

              <div className={invalidFields.has("confirmPassword") ? "input-field-error" : ""}>
                <PasswordInputField
                  name="confirmPassword"
                  placeholder="Confirm Password"
                  value={form.confirmPassword}
                  onChange={handleChange}
                  onBlur={handleBlur}
                  title="Please confirm your password"
                />
              </div>

              <div className="button-wrapper">
                <Button type="submit" variant="yellow" disabled={isLoading}>
                  {isLoading ? <LoadingButtonContent text="Signing up..." /> : "Sign Up"}
                </Button>
              </div>
            </form>
          </div>

          {/* Sign In Form */}
          <div className="form-container sign-in-container">
            <form onSubmit={handleSubmit} className="animated-form" noValidate>
              <div className="website-branding">
                <h2 className="brand-title">IELTS PHOBIC</h2>
              </div>
              
              {mode === "forgot" ? (
                <>
                  <h1>Forget Password</h1>
                  <p style={{ marginBottom: '20px', color: '#666' }}>Please enter your email to get the OTP reset password</p>
                </>
              ) : (
                <>
                  <h1>Sign in</h1>
                  <div className="social-container">
                    <button
                      type="button"
                      className="social google-btn"
                    >
                      <img src={google} alt="Google" className="social-img" />
                    </button>
                  </div>
                  <span>or use your account</span>
                </>
              )}
              
              {error && (mode === "login" || mode === "forgot") && <ErrorMessage message={error} icon={getErrorIcon(error)} />}
              {success && (mode === "login" || mode === "forgot") && <SuccessMessage message={success} />}

              {mode === "forgot" ? (
                <>
                  <div className={invalidFields.has("email") ? "input-field-error" : ""} style={{ position: 'relative' }}>
                    <FormInput
                      name="email"
                      type="email"
                      placeholder="Email"
                      value={form.email}
                      onChange={handleChange}
                      onBlur={handleBlur}
                      onFocus={handleEmailFocus}
                      title="Please enter a valid email address"
                      inputRef={emailInputRef}
                    />
                    <EmailGuidePopup
                      show={showEmailGuide}
                      position={emailPopupPosition}
                      popupRef={emailGuidePopupRef}
                      requirements={emailRequirements}
                      onClose={() => setShowEmailGuide(false)}
                    />
                  </div>
                  <div className="button-wrapper">
                    <Button type="submit" variant="yellow" disabled={isLoading}>
                      {isLoading ? <LoadingButtonContent text="Sending..." /> : "Send Reset Link"}
                    </Button>
                  </div>
                  <a href="#" className="forgot-link" onClick={(e) => { e.preventDefault(); switchMode("login"); }}>
                    ← Back to login
                  </a>
                </>
              ) : (
                <>
                  <div className={invalidFields.has("email") ? "input-field-error" : ""} style={{ position: 'relative' }}>
                    <FormInput
                      name="email"
                      type="email"
                      placeholder="Email"
                      value={form.email}
                      onChange={handleChange}
                      onBlur={handleBlur}
                      onFocus={handleEmailFocus}
                      title="Please enter a valid email address"
                      inputRef={emailInputRef}
                    />
                    <EmailGuidePopup
                      show={showEmailGuide}
                      position={emailPopupPosition}
                      popupRef={emailGuidePopupRef}
                      requirements={emailRequirements}
                      onClose={() => setShowEmailGuide(false)}
                    />
                  </div>

                  <div className={invalidFields.has("password") ? "input-field-error" : ""}>
                    <PasswordInputField
                      name="password"
                      placeholder="Password"
                      value={form.password}
                      onChange={handleChange}
                      onBlur={handleBlur}
                      title="Password is required"
                    />
                  </div>

                  <a href="#" className="forgot-link" onClick={(e) => { e.preventDefault(); switchMode("forgot"); }}>
                    Forgot your password?
                  </a>

                  <div className="button-wrapper">
                    <Button type="submit" variant="yellow" disabled={isLoading}>
                      {isLoading ? <LoadingButtonContent text="Signing in..." /> : "Sign In"}
                    </Button>
                  </div>
                </>
              )}
            </form>
          </div>

          {/* Overlay Panel */}
          <div className="overlay-container">
            <div className="overlay">
              <div className="overlay-panel overlay-left">
                <h1>Welcome Back!</h1>
                <p>
                  To keep connected with us please login with your personal info
                </p>
                <Button
                  className="ghost"
                  id="signIn"
                  onClick={() => switchMode("login")}
                >
                  Sign In
                </Button>
              </div>
              <div className="overlay-panel overlay-right">
                <h1>Hello, Friend!</h1>
                <p>Enter your personal details and start journey with us</p>
                <Button
                  className="ghost"
                  id="signUp"
                  onClick={() => switchMode("register")}
                >
                  Sign Up
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  };

  export default Login;
