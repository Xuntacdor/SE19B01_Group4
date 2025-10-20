import React, { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { createVipTransaction } from "../../Services/TransactionApi";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import styles from "./PaymentPage.module.css";

export default function PaymentPage() {
  const { state } = useLocation();
  const plan = state?.plan;
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);

  // Nếu người dùng reload trang, không còn state => quay lại danh sách gói
  if (!plan) {
    navigate("/vipplans");
    return null;
  }

  function handleConfirm() {
    setLoading(true);
    createVipTransaction(plan.vipPlanId, "QR")
      .then(() => {
        alert(
          "Payment created! Please complete transfer then wait for admin approval."
        );
        navigate("/profile", { state: { activeTab: "payment" } });
      })
      .catch(() => {
        alert("Failed to create transaction.");
      })
      .finally(() => {
        setLoading(false);
      });
  }

  return (
    <AppLayout title="Confirm Payment" sidebar={<GeneralSidebar />}>
      <div className={styles.page}>
        <h2>Confirm Payment</h2>
        <p>Plan: {plan.planName}</p>
        <p>Price: {plan.price.toLocaleString()} VND</p>

        <div className={styles.qrBox}>
          <img src="/qr-admin-bank.png" alt="QR Code" />
          <p>Scan this QR to pay via your bank app.</p>
        </div>

        <button
          onClick={handleConfirm}
          className={styles.confirmBtn}
          disabled={loading}
        >
          {loading ? "Processing..." : "I've Paid"}
        </button>
      </div>
    </AppLayout>
  );
}
