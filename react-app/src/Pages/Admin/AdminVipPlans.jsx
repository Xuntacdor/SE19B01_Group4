import React, { useEffect, useState } from "react";
import AppLayout from "../../Components/Layout/AppLayout";
import AdminNavbar from "../../Components/Admin/AdminNavbar";
import {
  getAllVipPlans,
  createVipPlan,
  updateVipPlan,
  deleteVipPlan,
} from "../../Services/VipPlanApi";
import styles from "./AdminVipPlans.module.css";

export default function AdminVipPlans() {
  const [plans, setPlans] = useState([]);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState({
    planName: "",
    durationDays: "",
    price: "",
    description: "",
  });

  useEffect(() => {
    reload();
  }, []);

  function reload() {
    getAllVipPlans()
      .then((res) => setPlans(res.data))
      .catch(() => setPlans([]));
  }

  function handleSubmit(e) {
    e.preventDefault();
    const data = {
      planName: form.planName.trim(),
      durationDays: Number(form.durationDays),
      price: Number(form.price),
      description: form.description.trim(),
    };

    if (!data.planName || !data.durationDays || !data.price) {
      alert("Please fill in all required fields.");
      return;
    }

    const req = editing
      ? updateVipPlan(editing.vipPlanId, data)
      : createVipPlan(data);

    req
      .then(() => {
        reload();
        setForm({ planName: "", durationDays: "", price: "", description: "" });
        setEditing(null);
      })
      .catch(() => alert("Failed to save plan."));
  }

  function handleEdit(p) {
    setEditing(p);
    setForm({
      planName: p.planName,
      durationDays: p.durationDays,
      price: p.price,
      description: p.description || "",
    });
  }

  function handleDelete(id) {
    if (!window.confirm("Delete this VIP plan?")) return;
    deleteVipPlan(id)
      .then(() => reload())
      .catch(() => alert("Failed to delete."));
  }

  return (
    <AppLayout sidebar={<AdminNavbar />} title="VIP Plans">
      <div className={styles.page}>
        <div className={styles.container}>
          <h2 className={styles.title}>Manage VIP Plans</h2>

          <form className={styles.form} onSubmit={handleSubmit}>
            <input
              type="text"
              placeholder="Plan name (e.g., 1 Month)"
              value={form.planName}
              onChange={(e) => setForm({ ...form, planName: e.target.value })}
            />
            <input
              type="number"
              placeholder="Duration (days)"
              value={form.durationDays}
              onChange={(e) =>
                setForm({ ...form, durationDays: e.target.value })
              }
            />
            <input
              type="number"
              placeholder="Price (VND)"
              value={form.price}
              onChange={(e) => setForm({ ...form, price: e.target.value })}
            />
            <input
              type="text"
              placeholder="Description (optional)"
              value={form.description}
              onChange={(e) =>
                setForm({ ...form, description: e.target.value })
              }
            />
            <button type="submit" className={styles.submitBtn}>
              {editing ? "Update" : "Add"} Plan
            </button>
            {editing && (
              <button
                type="button"
                className={styles.cancelBtn}
                onClick={() => {
                  setEditing(null);
                  setForm({
                    planName: "",
                    durationDays: "",
                    price: "",
                    description: "",
                  });
                }}
              >
                Cancel
              </button>
            )}
          </form>

          <table className={styles.table}>
            <thead>
              <tr>
                <th>ID</th>
                <th>Plan</th>
                <th>Duration</th>
                <th>Price (VND)</th>
                <th>Description</th>
                <th>Created</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {plans.map((p) => (
                <tr key={p.vipPlanId}>
                  <td>{p.vipPlanId}</td>
                  <td>{p.planName}</td>
                  <td>{p.durationDays} days</td>
                  <td>{p.price.toLocaleString()}</td>
                  <td>{p.description || "-"}</td>
                  <td>{new Date(p.createdAt).toLocaleDateString("vi-VN")}</td>
                  <td>
                    <button
                      className={styles.editBtn}
                      onClick={() => handleEdit(p)}
                    >
                      Edit
                    </button>
                    <button
                      className={styles.deleteBtn}
                      onClick={() => handleDelete(p.vipPlanId)}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
              {plans.length === 0 && (
                <tr>
                  <td colSpan="7" className={styles.empty}>
                    No VIP plans found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </AppLayout>
  );
}
