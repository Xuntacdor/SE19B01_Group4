import axios from "axios";

const API = axios.create({
  baseURL: "/api/vipplans",
  withCredentials: true,
});

export function getAllVipPlans() {
  return API.get("/");
}

export function createVipPlan(payload) {
  return API.post("/", payload);
}

export function updateVipPlan(id, payload) {
  return API.put(`/${id}`, payload);
}

export function deleteVipPlan(id) {
  return API.delete(`/${id}`);
}
