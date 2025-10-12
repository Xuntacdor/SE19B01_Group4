import { useEffect, useState } from "react";
import { getTransactions } from "../Services/TransactionApi";

export default function UseTransactions(initialQuery) {
  const [query, setQuery] = useState(
    initialQuery || {
      page: 1,
      pageSize: 20,
      sortBy: "createdAt",
      sortDir: "desc",
    }
  );

  const [state, setState] = useState({
    loading: false,
    data: [],
    total: 0,
    page: 1,
    pageSize: 20,
    error: null,
  });

  function load() {
    setState((prev) => ({ ...prev, loading: true, error: null }));

    getTransactions(query)
      .then((res) => {
        const result = res.data;
        setState({
          loading: false,
          data: result.items || [],
          total: result.total || 0,
          page: result.page || query.page,
          pageSize: result.pageSize || query.pageSize,
          error: null,
        });
      })
      .catch((err) => {
        console.error("Failed to load transactions:", err);
        setState((prev) => ({ ...prev, loading: false, error: err }));
      });
  }

  useEffect(() => {
    // debounce nhẹ để tránh gọi API dồn dập khi thay filter liên tục
    const timer = setTimeout(() => load(), 250);
    return () => clearTimeout(timer);
  }, [
    query.page,
    query.pageSize,
    query.sortBy,
    query.sortDir,
    query.filterStatus,
    query.filterType,
    query.dateFrom,
    query.dateTo,
    query.minAmount,
    query.maxAmount,
    query.search,
  ]);

  return { state, setQuery, reload: load };
}
