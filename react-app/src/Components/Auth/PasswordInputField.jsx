import React, { useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import styles from "./InputField.module.css";
import lockIcon from "../../assets/auth_lock.png";

const PasswordInputField = ({ placeholder, name, value, onChange, disabled = false }) => {
  const [showPassword, setShowPassword] = useState(false);

  const togglePasswordVisibility = () => {
    setShowPassword(!showPassword);
  };

  return (
    <div className={styles.formGroup}>
      <div className={styles.inputWrapper}>
        <img 
          src={lockIcon} 
          alt="Lock" 
          className={styles.inputIcon}
        />
        <input
          className={styles.passwordInputWithIcon}
          type={showPassword ? "text" : "password"}
          name={name}
          value={value}
          onChange={onChange}
          placeholder={placeholder}
          required
          disabled={disabled}
        />
        <button
          type="button"
          className={styles.passwordToggle}
          onClick={togglePasswordVisibility}
          tabIndex={-1}
          disabled={disabled}
        >
          {showPassword ? (
            <EyeOff size={18} className={styles.toggleIcon} />
          ) : (
            <Eye size={18} className={styles.toggleIcon} />
          )}
        </button>
      </div>
    </div>
  );
};

export default PasswordInputField;
