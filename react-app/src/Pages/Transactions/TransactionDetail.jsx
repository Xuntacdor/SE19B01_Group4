import React, { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import {
  getTransactionById,
  cancelTransaction,
  refundTransaction,
  approveTransaction,
} from "../../Services/TransactionApi";
import {
  CheckCircle,
  XCircle,
  RotateCcw,
  ArrowLeft,
  Loader2,
} from "lucide-react";
import styles from "./TransactionDetail.module.css";

export default function TransactionDetail() {
  const { id } = useParams();
  const [state, setState] = useState({
    loading: true,
    data: null,
    error: null,
  });

  function load() {
    setState({ loading: true, data: null, error: null });
    getTransactionById(id)
      .then(function (res) {
        setState({ loading: false, data: res.data, error: null });
      })
      .catch(function (err) {
        console.error("Failed to load transaction:", err);
        setState({ loading: false, data: null, error: err });
      });
  }

  useEffect(
    function () {
      if (id) load();
    },
    [id]
  );

  function onApprove() {
    if (!window.confirm("Approve this pending transaction?")) return;
    approveTransaction(id)
      .then(function () {
        load();
      })
      .catch(function () {
        alert("Failed to approve transaction.");
      });
  }

  function onCancel() {
    if (!window.confirm("Cancel this transaction?")) return;
    cancelTransaction(id)
      .then(function () {
        load();
      })
      .catch(function () {
        alert("Failed to cancel transaction.");
      });
  }

  function onRefund() {
    if (!window.confirm("Refund this transaction?")) return;
    refundTransaction(id)
      .then(function () {
        load();
      })
      .catch(function () {
        alert("Failed to refund transaction.");
      });
  }

  if (state.loading)
    return (
      <div className={styles.page}>
        <div className={styles.container}>
          <Loader2 className={styles.spinner + " animate-spin"} />
          <p>Loading transaction...</p>
        </div>
      </div>
    );

  if (state.error || !state.data)
    return (
      <div className={styles.page}>
        <div className={styles.container}>
          <p style={{ color: "#b91c1c" }}>Failed to load transaction.</p>
          <Link to="/admin/transactions" className={styles.back}>
            <ArrowLeft size={16} /> Back to list
          </Link>
        </div>
      </div>
    );

  var t = state.data;

  return (
    <div className={styles.page}>
      <div className={styles.container}>
        <div className={styles.header}>
          <Link className={styles.back} to="/admin/transactions">
            <ArrowLeft size={18} />
            Back
          </Link>
          <h2 className={styles.title}>Transaction #{t.transactionId}</h2>
        </div>

        <div className={styles.card}>
          <div className={styles.detailsGrid}>
            <div className={styles.label}>User ID</div>
            <div className={styles.value}>{t.userId}</div>

            <div className={styles.label}>Status</div>
            <div className={styles.value}>
              <span className={styles.statusBadge} data-status={t.status}>
                {t.status === "FAILED" ? "Canceled" : t.status}
              </span>
            </div>

            <div className={styles.label}>Amount</div>
            <div className={styles.value}>
              {Number(t.amount).toLocaleString("vi-VN")} {t.currency}
            </div>

            <div className={styles.label}>Payment</div>
            <div className={styles.value}>{t.paymentMethod || "-"}</div>

            <div className={styles.label}>Reference</div>
            <div className={styles.value}>{t.providerTxnId || "-"}</div>

            <div className={styles.label}>Purpose</div>
            <div className={styles.value}>{t.purpose}</div>

            <div className={styles.label}>Created</div>
            <div className={styles.value}>
              {new Date(t.createdAt).toLocaleString("vi-VN")}
            </div>
          </div>

          <div className={styles.actions}>
            <button
              className={styles.actionBtn + " " + styles.approveBtn}
              onClick={onApprove}
              disabled={t.status !== "PENDING"}
            >
              <CheckCircle size={18} /> Approve
            </button>

            <button
              className={styles.actionBtn + " " + styles.cancelBtn}
              onClick={onCancel}
              disabled={t.status !== "PENDING"}
            >
              <XCircle size={18} /> Cancel
            </button>

            <button
              className={styles.actionBtn + " " + styles.refundBtn}
              onClick={onRefund}
              disabled={t.status !== "PAID"}
            >
              <RotateCcw size={18} /> Refund
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
