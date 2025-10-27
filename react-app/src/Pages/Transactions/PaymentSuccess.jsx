// src/Pages/Transaction/PaymentSuccess.jsx
import React, { useEffect } from "react";
import { useSearchParams, Link } from "react-router-dom";
import { CheckCircle, XCircle, PartyPopper } from "lucide-react";
import confetti from "canvas-confetti";
import styles from "./PaymentSuccess.module.css";

export default function PaymentSuccess() {
  const [params] = useSearchParams();
  const success = params.get("success");
  const canceled = params.get("canceled");

  useEffect(() => {
    if (success) {
      const duration = 2 * 1000;
      const end = Date.now() + duration;

      const frame = () => {
        confetti({
          particleCount: 4,
          angle: 60,
          spread: 60,
          origin: { x: 0 },
          colors: ["#2563eb", "#facc15", "#22c55e", "#3b82f6"],
        });
        confetti({
          particleCount: 4,
          angle: 120,
          spread: 60,
          origin: { x: 1 },
          colors: ["#2563eb", "#facc15", "#22c55e", "#3b82f6"],
        });
        if (Date.now() < end) requestAnimationFrame(frame);
      };
      frame();
    }
  }, [success]);

  return (
    <div className={styles.wrapper}>
      {success && (
        <div className={`${styles.message} ${styles.success}`}>
          <PartyPopper className={styles.icon} size={60} />
          <h2>Payment Successful!</h2>
          <p>
            You have been upgraded to a VIP account. Thank you for your support!
          </p>
          <Link to="/profile" className={styles.link}>
            <CheckCircle size={18} />
            <span>View account information</span>
          </Link>
        </div>
      )}

      {canceled && (
        <div className={`${styles.message} ${styles.canceled}`}>
          <XCircle className={styles.icon} size={60} />
          <h2>Payment Canceled</h2>
          <p>You can try again anytime.</p>
          <Link to="/vipplans" className={styles.link}>
            <XCircle size={18} />
            <span>Return to select VIP plan</span>
          </Link>
        </div>
      )}
    </div>
  );
}
