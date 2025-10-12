import React, { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import AppLayout from "../../Components/Layout/AppLayout";
import AdminNavbar from "../../Components/Admin/AdminNavbar";
import UseTransactions from "../../Hook/UseTransactions";
import { exportCsv } from "../../Services/TransactionApi";
import { getTodayVietnamISO } from "../../utils/date";
import {
  ArrowLeft,
  ArrowRight,
  FileDown,
  RotateCcw,
  Loader2,
} from "lucide-react";
import styles from "./TransactionList.module.css";

export default function TransactionList() {
  const [local, setLocal] = useState({
    search: "",
    sortBy: "createdAt",
    sortDir: "desc",
    pageSize: 20,
    date: getTodayVietnamISO(),
    status: "",
  });

  const { state, setQuery } = UseTransactions({
    page: 1,
    pageSize: local.pageSize,
    sortBy: local.sortBy,
    sortDir: local.sortDir,
    search: local.search,
    dateFrom: `${local.date}T00:00:00`,
    dateTo: `${local.date}T23:59:59`,
    filterStatus: local.status, // ✅ dùng field mới
  });

  function resetFilters() {
    const reset = {
      search: "",
      sortBy: "createdAt",
      sortDir: "desc",
      pageSize: 20,
      date: getTodayVietnamISO(),
      status: "",
    };
    setLocal(reset);
    setQuery({
      page: 1,
      pageSize: reset.pageSize,
      sortBy: reset.sortBy,
      sortDir: reset.sortDir,
      search: reset.search,
      dateFrom: `${reset.date}T00:00:00`,
      dateTo: `${reset.date}T23:59:59`,
      filterStatus: reset.status,
    });
  }

  function changeDate(delta) {
    if (!local.date) return;
    const current = new Date(local.date);
    current.setDate(current.getDate() + delta);
    const newDate = current.toISOString().split("T")[0];
    setLocal({ ...local, date: newDate });
    setQuery((prev) => ({
      ...prev,
      dateFrom: `${newDate}T00:00:00`,
      dateTo: `${newDate}T23:59:59`,
      page: 1,
    }));
  }

  function onExport() {
    exportCsv({
      search: local.search,
      sortBy: local.sortBy,
      sortDir: local.sortDir,
      dateFrom: `${local.date}T00:00:00`,
      dateTo: `${local.date}T23:59:59`,
      filterStatus: local.status, // ✅ field mới
    })
      .then((res) => {
        const blob = new Blob([res.data], { type: "text/csv" });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = `transactions_${new Date()
          .toISOString()
          .replace(/[:.]/g, "-")}.csv`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);
      })
      .catch(() => {});
  }

  const columns = useMemo(
    () => [
      { key: "transactionId", label: "ID" },
      { key: "username", label: "USERNAME" },
      { key: "amount", label: "MONEY (VND)" },
      { key: "createdAt", label: "DATE" },
      { key: "paymentMethod", label: "TYPE" },
      { key: "status", label: "STATUS" },
    ],
    []
  );

  return (
    <AppLayout title="Transactions" sidebar={<AdminNavbar />}>
      <div className={styles.page}>
        <div className={styles.container}>
          <div className={styles.header}>
            <h2 className={styles.title}>Transaction Lists</h2>
          </div>

          {/* Toolbar */}
          <div className={styles.toolbar}>
            <div className={styles.filterGroup}>
              <div className={styles.filterInline}>
                <span className={styles.filterLabel}>Filter By</span>
                <input
                  type="date"
                  className={styles.dateInput}
                  value={local.date}
                  onChange={(e) => {
                    const newDate = e.target.value;
                    setLocal({ ...local, date: newDate });
                    setQuery((prev) => ({
                      ...prev,
                      dateFrom: `${newDate}T00:00:00`,
                      dateTo: `${newDate}T23:59:59`,
                      page: 1,
                    }));
                  }}
                />
              </div>

              <select
                className={styles.select}
                value={local.status}
                onChange={(e) => {
                  const newStatus = e.target.value;
                  setLocal({ ...local, status: newStatus });
                  setQuery((prev) => ({
                    ...prev,
                    filterStatus: newStatus, // ✅ đổi field
                    page: 1,
                  }));
                }}
              >
                <option value="">Transaction Status</option>
                <option value="PENDING">Pending</option>
                <option value="COMPLETED">Completed</option>
                <option value="REFUNDED">Refunded</option>
                <option value="CANCELED">Canceled</option>
                <option value="FAILED">Failed</option>
                <option value="PAID">Paid</option>
              </select>

              <button className={styles.reset} onClick={resetFilters}>
                <RotateCcw size={16} />
                Reset Filter
              </button>
            </div>

            <div className={styles.exportGroup}>
              <button className={styles.exportButton} onClick={onExport}>
                <FileDown size={16} />
                Export CSV
              </button>
            </div>
          </div>

          {/* Table */}
          <div className={`${styles.card} ${styles.tableWrapper}`}>
            {state.loading ? (
              <div style={{ textAlign: "center", padding: "20px" }}>
                <Loader2 size={24} className="animate-spin" />
                <p>Loading...</p>
              </div>
            ) : (
              <table className={styles.table}>
                <thead>
                  <tr>
                    {columns.map((c) => (
                      <th key={c.key} className={styles.th}>
                        {c.label}
                      </th>
                    ))}
                    <th className={styles.th}></th>
                  </tr>
                </thead>
                <tbody>
                  {(state.data || []).map((row) => (
                    <tr key={row.transactionId}>
                      {columns.map((c) => (
                        <td key={c.key} className={styles.td}>
                          {c.key === "status"
                            ? renderStatusBadge(row.status)
                            : formatCell(c.key, row)}
                        </td>
                      ))}
                      <td className={styles.td}>
                        <Link
                          className={styles.rowLink}
                          to={`/admin/transactions/${row.transactionId}`}
                        >
                          View
                        </Link>
                      </td>
                    </tr>
                  ))}
                  {!state.loading && (state.data || []).length === 0 && (
                    <tr>
                      <td colSpan={columns.length + 1} className={styles.empty}>
                        No data
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            )}
          </div>

          {/* Footer */}
          <div className={styles.navFooter}>
            <button
              className={styles.navBtn}
              onClick={() => changeDate(-1)}
              disabled={!local.date}
            >
              <ArrowLeft size={18} />
              Prev. Date
            </button>

            <button
              className={styles.navBtn}
              onClick={() => changeDate(1)}
              disabled={!local.date}
            >
              Next Date
              <ArrowRight size={18} />
            </button>
          </div>
        </div>
      </div>
    </AppLayout>
  );
}

function renderStatusBadge(status) {
  const normalized = status?.toUpperCase() || "";
  const map = {
    PENDING: { text: "Pending", color: "#854d0e", bg: "#fef9c3" },
    PROCESSING: { text: "Processing", color: "#4c1d95", bg: "#ede9fe" },
    COMPLETED: { text: "Completed", color: "#065f46", bg: "#d1fae5" },
    PAID: { text: "Paid", color: "#065f46", bg: "#dcfce7" },
    REFUNDED: { text: "Refunded", color: "#1e40af", bg: "#dbeafe" },
    CANCELED: { text: "Canceled", color: "#7f1d1d", bg: "#fee2e2" },
    FAILED: { text: "Failed", color: "#991b1b", bg: "#fee2e2" },
  };
  const style = map[normalized] || {
    text: status || "Unknown",
    color: "#444",
    bg: "#f3f4f6",
  };
  return (
    <span
      style={{
        background: style.bg,
        color: style.color,
        borderRadius: "999px",
        padding: "4px 10px",
        fontSize: "13px",
        fontWeight: 600,
        textTransform: "capitalize",
      }}
    >
      {style.text}
    </span>
  );
}

function formatCell(key, row) {
  if (key === "createdAt")
    return new Date(row.createdAt).toLocaleString("vi-VN");
  if (key === "amount")
    return (
      Number(row.amount).toLocaleString("vi-VN", {
        minimumFractionDigits: 0,
        maximumFractionDigits: 0,
      }) + "đ"
    );
  return row[key] || "-";
}
