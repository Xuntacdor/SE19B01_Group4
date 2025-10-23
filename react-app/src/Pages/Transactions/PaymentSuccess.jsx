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
      // 🎊 Tạo hiệu ứng confetti khi thanh toán thành công
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
          <h2>Thanh toán thành công!</h2>
          <p>Bạn đã được nâng cấp lên tài khoản VIP. Cảm ơn bạn đã ủng hộ!</p>
          <Link to="/profile" className={styles.link}>
            <CheckCircle size={18} />
            <span>Xem thông tin tài khoản</span>
          </Link>
        </div>
      )}

      {canceled && (
        <div className={`${styles.message} ${styles.canceled}`}>
          <XCircle className={styles.icon} size={60} />
          <h2>Thanh toán bị hủy</h2>
          <p>Bạn có thể thử lại bất cứ lúc nào.</p>
          <Link to="/vipplans" className={styles.link}>
            <XCircle size={18} />
            <span>Quay lại chọn gói VIP</span>
          </Link>
        </div>
      )}
    </div>
  );
}
