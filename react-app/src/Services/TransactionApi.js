import axios from "axios";
import { getApiUrl } from "../config/api";

const API = axios.create({
  baseURL: getApiUrl("transactions"),
  withCredentials: true,
});

// chỉ cho phép 3 trường hợp sort hợp lệ
const allowedSortBy = { createdAt: 1, amount: 1, status: 1 };

function sanitizeQuery(q) {
  const sortBy = allowedSortBy[q.sortBy] ? q.sortBy : "createdAt";
  const sortDir = q.sortDir === "asc" ? "asc" : "desc";
  const page = q.page > 0 ? q.page : 1;
  const pageSize = q.pageSize > 0 && q.pageSize <= 100 ? q.pageSize : 20;

  // backend dùng filterStatus, filterType thay vì status/type
  const filterStatus = q.filterStatus || q.status || "";
  const filterType = q.filterType || q.type || "";

  return {
    ...q,
    sortBy,
    sortDir,
    page,
    pageSize,
    filterStatus,
    filterType,
  };
}

export function getTransactions(query, options = {}) {
  const params = sanitizeQuery(query || {});
  return API.get("/", { params, ...options });
}

export function getTransactionById(id) {
  return API.get(`/${id}`);
}

export function createTransaction(payload) {
  return API.post("/", payload);
}

export function cancelTransaction(id) {
  return API.post(`/${id}/cancel`);
}

export function refundTransaction(id) {
  return API.post(`/${id}/refund`);
}

export function approveTransaction(id) {
  return API.post(`/${id}/approve`);
}

export function exportCsv(query) {
  const params = sanitizeQuery(query || {});
  return API.get("/export", { params, responseType: "blob" });
}
export function createVipTransaction(planId, paymentMethod) {
  return API.post("/create", {
    planId,
    paymentMethod,
    purpose: "VIP",
  });
}
