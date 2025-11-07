import React from "react";
import styles from "./InputField.module.css";

const InputField = ({ icon, type="text", placeholder, name, value, onChange, onBlur, required = false, minLength, pattern, title }) => {
  return (
    <div className={styles.formGroup}>
      <div className={styles.inputWrapper}>
        {icon && <img src={icon} alt="" className={styles.inputIcon} />}
        <input
          className={icon ? styles.input : styles.inputNoIcon}
          type={type}
          name={name}
          value={value}
          onChange={onChange}
          onBlur={onBlur}
          placeholder={placeholder}
          required={required}
          minLength={minLength}
          pattern={pattern}
          title={title}
        />
      </div>
    </div>
  );
};

export default InputField;
