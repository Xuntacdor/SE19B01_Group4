import React, { useEffect, useState } from "react";
import { getAllVipPlans } from "../../Services/VipPlanApi";
import { useNavigate } from "react-router-dom";
import styles from "./VipPlans.module.css";

// 🧩 Import layout và sidebar
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";

export default function VipPlans() {
  const [plans, setPlans] = useState([]);
  const navigate = useNavigate();

  useEffect(() => {
    getAllVipPlans()
      .then((res) => setPlans(res.data))
      .catch((err) => {
        console.error("Failed to load VIP plans", err);
      });
  }, []);

  function handleSelect(plan) {
    navigate(`/transactions/payment/${plan.vipPlanId}`, { state: { plan } });
  }

  return (
    <AppLayout sidebar={<GeneralSidebar />} title="VIP Plans">
      <div className={styles.page}>
        <h1 className={styles.title}>Choose Your VIP Plan</h1>
        <div className={styles.grid}>
          {plans.map((p) => (
            <div key={p.vipPlanId} className={styles.card}>
              <h2>{p.planName}</h2>
              <p>{p.description}</p>
              <p>
                <b>{p.price.toLocaleString()} VND</b> / {p.durationDays} days
              </p>
              <button onClick={() => handleSelect(p)}>Select</button>
            </div>
          ))}
        </div>
      </div>
    </AppLayout>
  );
}
