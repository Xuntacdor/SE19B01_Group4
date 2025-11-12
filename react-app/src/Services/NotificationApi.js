import axios from "axios";
import { getApiUrl } from "../config/api";

const API = axios.create({
  baseURL: getApiUrl("notification"),
  withCredentials: true,
});

export function getNotifications() {
  return API.get("/");
}

export function markAsRead(notificationId) {
  return API.put(`/${notificationId}/read`);
}

export function deleteNotification(notificationId) {
  return API.delete(`/${notificationId}`);
}


