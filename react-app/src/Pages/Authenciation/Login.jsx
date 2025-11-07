import React, { useState, useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import FormInput from "../../Components/Auth/InputField";
import PasswordInputField from "../../Components/Auth/PasswordInputField";
import Button from "../../Components/Auth/Button";
import { login, register, loginWithGoogle, forgotPassword } from "../../Services/AuthApi.js";
import google from "../../assets/google.png";
import "./Login.css";
import { useNavigate } from "react-router-dom";
import { 
  User, 
  Lock, 
  Mail, 
  AlertCircle, 
  CheckCircle, 
  Loader2 
} from "lucide-react";

// Validation Popup Component - Login Form Specific
const LoginFormValidationPopup = ({ message, targetElement }) => {
  const loginFormPopupRef = useRef(null);
  const positionUpdateTimeoutRef = useRef(null);
  
  // Calculate initial position immediately based on target element
  const getInitialPosition = () => {
    if (!targetElement) return { top: 0, left: 0, isAbove: true };
    
    const rect = targetElement.getBoundingClientRect();
    const estimatedPopupHeight = 60;
    const estimatedPopupWidth = 280;
    
    const inputCenterX = rect.left + (rect.width / 2);
    let left = inputCenterX - (estimatedPopupWidth / 2);
    
    const horizontalPadding = 10;
    if (left < horizontalPadding) left = horizontalPadding;
    if (left + estimatedPopupWidth > window.innerWidth - horizontalPadding) {
      left = window.innerWidth - estimatedPopupWidth - horizontalPadding;
    }
    
    const verticalSpacing = 12;
    let top = rect.top - estimatedPopupHeight - verticalSpacing;
    let isAbove = true;
    
    const verticalPadding = 10;
    if (top < verticalPadding) {
      top = rect.bottom + verticalSpacing;
      isAbove = false;
    }
    
    top = Math.max(verticalPadding, top);
    
    return { top, left, isAbove };
  };
  
  const [loginFormPopupPosition, setLoginFormPopupPosition] = useState(() => getInitialPosition());

  useEffect(() => {
    if (!targetElement || !message) return;
    
    // Update initial position when targetElement changes
    const initialPos = getInitialPosition();
    setLoginFormPopupPosition(initialPos);
    
    const updateLoginFormPopupPosition = () => {
      if (!targetElement) return;
      
      // Get the input element's position relative to the viewport
      const rect = targetElement.getBoundingClientRect();
      
      // Get popup dimensions - wait for actual dimensions
      const popupElement = loginFormPopupRef.current;
      if (!popupElement) {
        // If popup not rendered yet, schedule another update
        requestAnimationFrame(updateLoginFormPopupPosition);
        return;
      }
      
      // Force a reflow to ensure dimensions are calculated
      popupElement.offsetHeight;
      
      const popupHeight = popupElement.offsetHeight || 60;
      const popupWidth = popupElement.offsetWidth || 280;
      
      // Calculate center position of input field
      const inputCenterX = rect.left + (rect.width / 2);
      
      // Position popup centered above the input field
      let left = inputCenterX - (popupWidth / 2);
      
      // Ensure popup doesn't go off screen horizontally
      const horizontalPadding = 10;
      if (left < horizontalPadding) {
        left = horizontalPadding;
      } else if (left + popupWidth > window.innerWidth - horizontalPadding) {
        left = window.innerWidth - popupWidth - horizontalPadding;
      }
      
      // Position popup above the input field with spacing
      const verticalSpacing = 12;
      let top = rect.top - popupHeight - verticalSpacing;
      let isAbove = true;
      
      // Check if there's enough space above
      const verticalPadding = 10;
      if (top < verticalPadding) {
        // Not enough space above, position below instead
        top = rect.bottom + verticalSpacing;
        isAbove = false;
      }
      
      // Ensure popup doesn't go below viewport
      if (top + popupHeight > window.innerHeight - verticalPadding) {
        top = window.innerHeight - popupHeight - verticalPadding;
      }
      
      // Ensure top is never negative
      top = Math.max(verticalPadding, top);
      
      setLoginFormPopupPosition({
        top: top,
        left: left,
        isAbove: isAbove
      });
    };

    // Clear any pending timeouts
    if (positionUpdateTimeoutRef.current) {
      clearTimeout(positionUpdateTimeoutRef.current);
    }

    // Wait for next frame to ensure popup is in DOM
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        updateLoginFormPopupPosition();
        
        // Update again after popup is fully rendered with accurate dimensions
        positionUpdateTimeoutRef.current = setTimeout(() => {
          updateLoginFormPopupPosition();
        }, 100);
      });
    });
    
    // Event handlers for dynamic updates
    const handleScroll = () => {
      updateLoginFormPopupPosition();
    };
    
    const handleResize = () => {
      updateLoginFormPopupPosition();
    };
    
    // Use passive listeners for better performance
    window.addEventListener('scroll', handleScroll, { passive: true, capture: true });
    window.addEventListener('resize', handleResize, { passive: true });

    return () => {
      window.removeEventListener('scroll', handleScroll, { capture: true });
      window.removeEventListener('resize', handleResize);
      if (positionUpdateTimeoutRef.current) {
        clearTimeout(positionUpdateTimeoutRef.current);
      }
    };
  }, [targetElement, message]);

  if (!message || !targetElement) {
    return null;
  }

  const popupStyle = {
    position: 'fixed',
    top: `${loginFormPopupPosition.top}px`,
    left: `${loginFormPopupPosition.left}px`,
    zIndex: 10000,
    pointerEvents: 'none',
    visibility: loginFormPopupPosition.top > 0 ? 'visible' : 'hidden' // Hide if position not calculated yet
  };
  
  const popupContent = (
    <div 
      ref={loginFormPopupRef}
      className="login-form-validation-popup"
      style={popupStyle}
    >
      <div className="login-form-validation-popup-content">
        <div className="login-form-validation-popup-icon">
          <AlertCircle size={20} />
        </div>
        <div className="login-form-validation-popup-message">{message}</div>
      </div>
      <div className={`login-form-validation-popup-arrow ${loginFormPopupPosition.isAbove ? 'arrow-down' : 'arrow-up'}`}></div>
    </div>
  );
  
  // Use portal to render popup at document body level to avoid overflow issues
  return createPortal(popupContent, document.body);
};

// Reusable Input with Popup Wrapper Component
const InputWithPopup = ({ 
  children, 
  fieldName, 
  showPopup, 
  popupMessage, 
  targetElement 
}) => (
  <div className="input-with-popup-wrapper">
    {children}
    {showPopup && (
      <LoginFormValidationPopup 
        message={popupMessage}
        targetElement={targetElement}
      />
    )}
  </div>
);

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

const Login = () => {
    const [mode, setMode] = useState("login");
    const [form, setForm] = useState({ email: "", password: "", username: "", confirmPassword: "" });
    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");
    const [isLoading, setIsLoading] = useState(false);
    const [validationErrors, setValidationErrors] = useState({});
    const [loginFormShowValidationPopup, setLoginFormShowValidationPopup] = useState({ field: null, message: "" });
    const [loginFormInvalidFieldRef, setLoginFormInvalidFieldRef] = useState(null);
    const navigate = useNavigate();

    // Handle URL parameters and sync container class
    useEffect(() => {
      const params = new URLSearchParams(window.location.search);
      const modeParam = params.get("mode");
      if (modeParam) setMode(modeParam);

      // Handle OAuth login success
      const loginSuccess = params.get("login");
      if (loginSuccess === "success" && params.get("email")) {
        const user = { 
          email: params.get("email"), 
          username: params.get("username"), 
          role: params.get("role") || "user" 
        };
        localStorage.setItem("user", JSON.stringify(user));
        const routes = { admin: "/admin/dashboard", moderator: "/moderator/dashboard" };
        navigate(routes[user.role] || "/home");
      }
    }, [navigate]);

    // Sync container class with mode changes
    useEffect(() => {
      const container = document.getElementById("animated-login-container");
      if (container) {
        container.classList.toggle("right-panel-active", mode === "register");
      }
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
        return !email.includes("@") 
          ? `Please include an '@' in the email address. '${email}' is missing an '@'.`
          : "Please enter a valid email address";
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

    // Helper to show validation popup
    const showValidationPopup = (fieldName, message, element) => {
      setLoginFormInvalidFieldRef(element);
      setLoginFormShowValidationPopup({ field: fieldName, message });
    };

    // Helper to clear validation popup
    const clearValidationPopup = (fieldName) => {
      if (loginFormShowValidationPopup.field === fieldName) {
        setLoginFormShowValidationPopup({ field: null, message: "" });
      }
    };

    const handleChange = (e) => {
      const fieldName = e.target.name;
      const fieldValue = e.target.value;
      
      setForm(prev => ({ ...prev, [fieldName]: fieldValue }));
      if (error) setError("");
      if (success) setSuccess("");
      
      if (validationErrors[fieldName]) {
        setValidationErrors(prev => ({ ...prev, [fieldName]: "" }));
      }
      
      // Re-validate confirmPassword when password changes
      if (fieldName === "password" && form.confirmPassword) {
        clearTimeout(window.validationTimeout_confirmPassword);
        window.validationTimeout_confirmPassword = setTimeout(() => {
          const confirmElement = document.querySelector('[name="confirmPassword"]');
          if (confirmElement) {
            const errorMsg = fieldValue !== form.confirmPassword ? "Passwords do not match" : null;
            errorMsg ? showValidationPopup("confirmPassword", errorMsg, confirmElement) : clearValidationPopup("confirmPassword");
          }
        }, 500);
      }
      
      // Real-time validation with debounce
      clearTimeout(window[`validationTimeout_${fieldName}`]);
      window[`validationTimeout_${fieldName}`] = setTimeout(() => {
        const errorMsg = validateField(fieldName, fieldValue);
        errorMsg ? showValidationPopup(fieldName, errorMsg, e.target) : clearValidationPopup(fieldName);
      }, 500);
    };

    const handleBlur = (e) => {
      const fieldName = e.target.name;
      const errorMsg = validateField(fieldName, e.target.value);
      errorMsg ? showValidationPopup(fieldName, errorMsg, e.target) : clearValidationPopup(fieldName);
    };

    const switchMode = (newMode) => {
      setMode(newMode);
      setError("");
      setSuccess("");
      setValidationErrors({});
      setForm({ email: "", password: "", username: "", confirmPassword: "" });
      setLoginFormShowValidationPopup({ field: null, message: "" });
    };

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
      
      console.error("Auth error:", statusCode, errorData || err.message);
    };

    const handleSubmit = (e) => {
      e.preventDefault();
      const formElement = e.target;
      
      setError("");
      setSuccess("");
      setValidationErrors({});
      setIsLoading(true);

      const errors = validateForm();
      
      if (Object.keys(errors).length > 0) {
        setIsLoading(false);
        setValidationErrors(errors);
        
        const firstErrorField = Object.keys(errors)[0];
        const inputElement = findInputElement(formElement, firstErrorField);
        
        if (inputElement) {
          showValidationPopup(firstErrorField, errors[firstErrorField], inputElement);
          requestAnimationFrame(() => {
            setTimeout(() => {
              inputElement.focus();
              setTimeout(() => setLoginFormShowValidationPopup({ field: null, message: "" }), 5000);
            }, 100);
          });
        } else {
          showValidationPopup(firstErrorField, errors[firstErrorField], null);
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
              admin: "/admin/dashboard",
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
                  onClick={loginWithGoogle}
                >
                  <img src={google} alt="Google" className="social-img" />
                </button>
              </div>
              <span>or use your email for registration</span>
              
              {error && mode === "register" && <ErrorMessage message={error} icon={getErrorIcon(error)} />}
              {success && mode === "register" && <SuccessMessage message={success} />}

              <InputWithPopup
                fieldName="username"
                showPopup={loginFormShowValidationPopup.field === "username"}
                popupMessage={loginFormShowValidationPopup.message}
                targetElement={loginFormInvalidFieldRef}
              >
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
              </InputWithPopup>

              <InputWithPopup
                fieldName="email"
                showPopup={loginFormShowValidationPopup.field === "email"}
                popupMessage={loginFormShowValidationPopup.message}
                targetElement={loginFormInvalidFieldRef}
              >
                <FormInput
                  name="email"
                  type="email"
                  placeholder="Email"
                  value={form.email}
                  onChange={handleChange}
                  onBlur={handleBlur}
                  title="Please enter a valid email address"
                />
              </InputWithPopup>

              <InputWithPopup
                fieldName="password"
                showPopup={loginFormShowValidationPopup.field === "password"}
                popupMessage={loginFormShowValidationPopup.message}
                targetElement={loginFormInvalidFieldRef}
              >
                <PasswordInputField
                  name="password"
                  placeholder="Password"
                  value={form.password}
                  onChange={handleChange}
                  onBlur={handleBlur}
                  minLength={6}
                  title="Password is required and must be at least 6 characters"
                />
              </InputWithPopup>

              <InputWithPopup
                fieldName="confirmPassword"
                showPopup={loginFormShowValidationPopup.field === "confirmPassword"}
                popupMessage={loginFormShowValidationPopup.message}
                targetElement={loginFormInvalidFieldRef}
              >
                <PasswordInputField
                  name="confirmPassword"
                  placeholder="Confirm Password"
                  value={form.confirmPassword}
                  onChange={handleChange}
                  onBlur={handleBlur}
                  title="Please confirm your password"
                />
              </InputWithPopup>

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
                      onClick={loginWithGoogle}
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
                  <InputWithPopup
                    fieldName="email"
                    showPopup={loginFormShowValidationPopup.field === "email"}
                    popupMessage={loginFormShowValidationPopup.message}
                    targetElement={loginFormInvalidFieldRef}
                  >
                    <FormInput
                      name="email"
                      type="email"
                      placeholder="Email"
                      value={form.email}
                      onChange={handleChange}
                      onBlur={handleBlur}
                      title="Please enter a valid email address"
                    />
                  </InputWithPopup>
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
                  <InputWithPopup
                    fieldName="email"
                    showPopup={loginFormShowValidationPopup.field === "email"}
                    popupMessage={loginFormShowValidationPopup.message}
                    targetElement={loginFormInvalidFieldRef}
                  >
                    <FormInput
                      name="email"
                      type="email"
                      placeholder="Email"
                      value={form.email}
                      onChange={handleChange}
                      onBlur={handleBlur}
                      title="Please enter a valid email address"
                    />
                  </InputWithPopup>

                  <InputWithPopup
                    fieldName="password"
                    showPopup={loginFormShowValidationPopup.field === "password"}
                    popupMessage={loginFormShowValidationPopup.message}
                    targetElement={loginFormInvalidFieldRef}
                  >
                    <PasswordInputField
                      name="password"
                      placeholder="Password"
                      value={form.password}
                      onChange={handleChange}
                      onBlur={handleBlur}
                      title="Password is required"
                    />
                  </InputWithPopup>

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
