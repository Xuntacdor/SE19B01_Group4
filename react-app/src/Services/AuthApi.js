import axios from "axios";
import { getApiUrl } from "../config/api";

const API = axios.create({
  baseURL: getApiUrl("auth"),
  withCredentials: true,
});

export function register(data) {
  return API.post("/register", data);
}

export function login(data) {
  return API.post("/login", data);
}

export function logout() {
  return API.post("/logout");
}

export function getMe() {
  return API.get("/me");
}



export function forgotPassword(email) {
  return API.post("/forgot-password", { email });
}

export function verifyOtp(email, otpCode) {
  return API.post("/verify-otp", { email, otpCode });
}

export function resetPassword(email, resetToken, newPassword, confirmPassword) {
  return API.post("/reset-password", { 
    email, 
    resetToken, 
    newPassword, 
    confirmPassword 
  });
}

export function changePassword(currentPassword, newPassword, confirmPassword) {
  return API.post("/change-password", { 
    currentPassword, 
    newPassword, 
    confirmPassword 
  });
}
