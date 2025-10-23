// src/Services/VipPaymentApi.js
import axios from "axios";

const API = axios.create({
  baseURL: "/api/vip",
  withCredentials: true,
});

/**
 * Gọi API tạo phiên thanh toán Stripe cho gói VIP.
 * @param {number} planId - ID của gói VIP cần thanh toán.
 * @returns {Promise<{ sessionUrl: string }>}
 */
export function createVipCheckout(planId) {
  return API.post("/pay", { planId });
}
