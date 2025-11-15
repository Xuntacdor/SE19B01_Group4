// src/Services/VipPaymentApi.js
import axios from "axios";
import { getApiUrl } from "../config/api";

const API = axios.create({
  baseURL: getApiUrl("vip"),
  withCredentials: true,
});

export function createVipCheckout(planId) {
  return API.post("/pay", { planId });
}
