import styles from "./Button.module.css";

export default function Button({ children, onClick, type = "button", variant = "yellow", disabled = false }) {
  return (
    <button
      type={type}
      onClick={onClick}
      className={`${styles.btn} ${styles[variant]}`}
      disabled={disabled}
    >
      {children}
    </button>
  );
}
