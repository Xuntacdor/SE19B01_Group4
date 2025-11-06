import React, { useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import styles from "./InputField.module.css";

const PasswordInputField = ({ placeholder, name, value, onChange, onBlur, disabled = false, required = true, minLength, title }) => {
  const [showPassword, setShowPassword] = useState(false);

  const togglePasswordVisibility = () => {
    setShowPassword(!showPassword);
  };

  return (
    <div className={styles.formGroup}>
      <div className={styles.inputWrapper}>
        <input
          className={styles.passwordInputNoIcon}
          type={showPassword ? "text" : "password"}
          name={name}
          value={value}
          onChange={onChange}
          onBlur={onBlur}
          placeholder={placeholder}
          required={required}
          minLength={minLength}
          title={title}
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
