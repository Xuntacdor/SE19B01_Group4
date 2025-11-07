import styles from "./Button.module.css";

export default function Button({ children, onClick, type = "button", variant = "yellow", disabled = false, className = "", id }) {
  return (
    <button
      type={type}
      onClick={onClick}
      className={`${styles.btn} ${styles[variant]} ${className}`}
      disabled={disabled}
      id={id}
    >
      {children}
    </button>
  );
}
