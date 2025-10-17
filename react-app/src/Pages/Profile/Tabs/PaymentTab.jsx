import React, { useEffect, useState } from "react";
import { getTransactions } from "../../../Services/TransactionApi";
import NothingFound from "../../../Components/Nothing/NothingFound";
import styles from "./PaymentTab.module.css";

export default function PaymentTab() {
  const [list, setList] = useState([]);

  useEffect(() => {
    getTransactions({ page: 1, pageSize: 20 })
      .then((res) => {
        if (res.data?.items) setList(res.data.items);
      })
      .catch(() => setList([]));
  }, []);

  return (
    <div className={styles.page}>
      <div className={styles.container}>
        <h2 className={styles.title}>Payment History</h2>

        {list.length === 0 ? (
          <NothingFound
            imageSrc="/src/assets/sad_cloud.png"
            title="No payments yet"
            message="When you purchase services, your transactions will appear here."
          />
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th className={styles.th}>ID</th>
                <th className={styles.th}>Amount</th>
                <th className={styles.th}>Status</th>
                <th className={styles.th}>Created</th>
              </tr>
            </thead>
            <tbody>
              {list.map((t) => (
                <tr key={t.transactionId} className={styles.row}>
                  <td className={styles.td}>{t.transactionId}</td>
                  <td className={styles.td}>
                    {t.amount.toLocaleString("vi-VN")} {t.currency}
                  </td>
                  <td className={styles.td}>
                    <span
                      className={`${styles.statusBadge} ${
                        styles[
                          "status" +
                            (t.status?.charAt(0).toUpperCase() +
                              t.status?.slice(1).toLowerCase())
                        ]
                      }`}
                    >
                      {t.status}
                    </span>
                  </td>
                  <td className={styles.td}>
                    {new Date(t.createdAt).toLocaleString("vi-VN")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
