import React, { useEffect, useState } from "react";
import { getAllVipPlans } from "../../Services/VipPlanApi";
import { createVipCheckout } from "../../Services/VipPaymentApi";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import NothingFound from "../../Components/Nothing/NothingFound";
import styles from "./VipPlans.module.css";
import { Crown, Loader2 } from "lucide-react";
import ConfirmationPopup from "../../Components/Common/ConfirmationPopup";

export default function VipPlans() {
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);

  const [popup, setPopup] = useState({
    show: false,
    message: "",
  });

  useEffect(() => {
    getAllVipPlans()
      .then((res) => setPlans(res.data))
      .catch((err) => {
        console.error("Error fetching VIP plans:", err);
        setPopup({
          show: true,
          message: "Failed to load VIP plans. Please try again.",
        });
      })
      .finally(() => setLoading(false));
  }, []);

  const handleBuyVip = async (planId) => {
    try {
      const { data } = await createVipCheckout(planId);
      if (data?.sessionUrl) {
        window.location.href = data.sessionUrl;
      } else {
        setPopup({
          show: true,
          message: "Unable to create checkout session. Please try again.",
        });
      }
    } catch (error) {
      console.error("Error creating Stripe checkout session:", error);
      setPopup({
        show: true,
        message: "Unable to connect to payment server.",
      });
    }
  };

  if (loading) {
    return (
      <AppLayout sidebar={<GeneralSidebar />} title="VIP Plans">
        <div className={styles.loadingContainer}>
          <Loader2 size={40} className={styles.spinner} />
          <p>Loading VIP plans...</p>
        </div>

        {/* Popup */}
        <ConfirmationPopup
          isOpen={popup.show}
          message={popup.message}
          title="Notification"
          confirmText="OK"
          cancelText="Close"
          onConfirm={() => setPopup({ show: false, message: "" })}
          onClose={() => setPopup({ show: false, message: "" })}
        />
      </AppLayout>
    );
  }

  if (!plans || plans.length === 0) {
    return (
      <AppLayout sidebar={<GeneralSidebar />} title="VIP Plans">
        <NothingFound
          title="No VIP plans available"
          message="Currently there are no VIP plans created."
          imageSrc="/src/assets/sad_cloud.png"
          actionLabel="Back to Home"
          to="/home"
        />

        {/* Popup */}
        <ConfirmationPopup
          isOpen={popup.show}
          message={popup.message}
          title="Notification"
          confirmText="OK"
          cancelText="Close"
          onConfirm={() => setPopup({ show: false, message: "" })}
          onClose={() => setPopup({ show: false, message: "" })}
        />
      </AppLayout>
    );
  }

  return (
    <AppLayout sidebar={<GeneralSidebar />} title="VIP Plans">
      <div className={styles.container}>
        <h1 className={styles.title}>
          <Crown className={styles.crownIcon} /> Choose Your VIP Plan
        </h1>

        <div className={styles.planGrid}>
          {plans.map((plan) => (
            <div key={plan.vipPlanId} className={styles.planCard}>
              <h3>{plan.name}</h3>
              <p className={styles.planDesc}>{plan.description}</p>
              <p className={styles.planPrice}>
                <strong>{plan.price.toLocaleString()} VND</strong> /{" "}
                {plan.durationDays} days
              </p>
              <button
                className={styles.buyBtn}
                onClick={() => handleBuyVip(plan.vipPlanId)}
              >
                <Crown size={18} style={{ marginRight: 6 }} /> Buy VIP
              </button>
            </div>
          ))}
        </div>
      </div>

      {/* Popup */}
      <ConfirmationPopup
        isOpen={popup.show}
        message={popup.message}
        title="Notification"
        confirmText="OK"
        cancelText="Close"
        onConfirm={() => setPopup({ show: false, message: "" })}
        onClose={() => setPopup({ show: false, message: "" })}
      />
    </AppLayout>
  );
}
