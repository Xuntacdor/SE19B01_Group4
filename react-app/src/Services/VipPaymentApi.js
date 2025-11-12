// src/Services/VipPaymentApi.js
import axios from "axios";
import { getApiUrl } from "../config/api";

const API = axios.create({
  baseURL: getApiUrl("vip"),
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
