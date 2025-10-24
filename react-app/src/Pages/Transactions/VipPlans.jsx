import React, { useEffect, useState } from "react";
import { getAllVipPlans } from "../../Services/VipPlanApi";
import { createVipCheckout } from "../../Services/VipPaymentApi";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import NothingFound from "../../Components/Nothing/NothingFound";
import styles from "./VipPlans.module.css";
import { Crown, Loader2 } from "lucide-react";

export default function VipPlans() {
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getAllVipPlans()
      .then((res) => setPlans(res.data))
      .catch((err) => console.error("Error fetching VIP plans:", err))
      .finally(() => setLoading(false));
  }, []);

  const handleBuyVip = async (planId) => {
    try {
      const { data } = await createVipCheckout(planId);
      if (data?.sessionUrl) {
        window.location.href = data.sessionUrl; // ✅ Redirect sang Stripe Checkout
      } else {
        alert("Không thể tạo phiên thanh toán. Vui lòng thử lại.");
      }
    } catch (error) {
      console.error("Lỗi khi tạo phiên thanh toán Stripe:", error);
      alert("Không thể kết nối đến máy chủ thanh toán.");
    }
  };

  // 🌀 Loading UI
  if (loading) {
    return (
      <AppLayout sidebar={<GeneralSidebar />} title="VIP Plans">
        <div className={styles.loadingContainer}>
          <Loader2 size={40} className={styles.spinner} />
          <p>Đang tải các gói VIP...</p>
        </div>
      </AppLayout>
    );
  }

  // 🧩 Empty state
  if (!plans || plans.length === 0) {
    return (
      <AppLayout sidebar={<GeneralSidebar />} title="VIP Plans">
        <NothingFound
          title="Không có gói VIP nào"
          message="Hiện chưa có gói VIP nào được tạo."
          imageSrc="/src/assets/sad_cloud.png"
          actionLabel="Quay lại trang chủ"
          to="/home"
        />
      </AppLayout>
    );
  }

  // 🎯 Main content
  return (
    <AppLayout sidebar={<GeneralSidebar />} title="VIP Plans">
      <div className={styles.container}>
        <h1 className={styles.title}>
          <Crown className={styles.crownIcon} /> Chọn gói VIP phù hợp
        </h1>

        <div className={styles.planGrid}>
          {plans.map((plan) => (
            <div key={plan.vipPlanId} className={styles.planCard}>
              <h3>{plan.name}</h3>
              <p className={styles.planDesc}>{plan.description}</p>
              <p className={styles.planPrice}>
                <strong>{plan.price.toLocaleString()} VND</strong> /{" "}
                {plan.durationDays} ngày
              </p>
              <button
                className={styles.buyBtn}
                onClick={() => handleBuyVip(plan.vipPlanId)}
              >
                <Crown size={18} style={{ marginRight: 6 }} /> Mua VIP
              </button>
            </div>
          ))}
        </div>
      </div>
    </AppLayout>
  );
}
