// src/services/examService.js
import axios from "axios";
import { getApiUrl } from "../config/api";

const API = axios.create({
  baseURL: getApiUrl("exam"),
  withCredentials: true,
});

const normalizeExam = (e) => ({
  examId: e.examId ?? e.ExamId,
  examName: e.examName ?? e.ExamName,
  examType: e.examType ?? e.ExamType,
  createdAt: e.createdAt ?? e.CreatedAt,
  backgroundImageUrl: e.backgroundImageUrl ?? e.BackgroundImageUrl,
});

// ====== EXAMS ======
export function getById(id) {
  return API.get(`/${id}`).then((res) => normalizeExam(res.data));
}

export function getAll() {
  return API.get("").then((res) => {
    console.log("ExamApi.getAll - Raw response:", res.data); // Debug log
    const list = Array.isArray(res.data)
      ? res.data.map(normalizeExam)
      : [];
    console.log("ExamApi.getAll - Normalized exams:", list); // Debug log
    return list;
  });
}

export function add(data) {
  console.log("ExamApi.add - Sending data:", data); // Debug log
  return API.post("", data).then((res) => {
    console.log("ExamApi.add - Response:", res.data); // Debug log
    return normalizeExam(res.data);
  });
}

export function update(id, data) {
  return API.put(`/${id}`, data).then((res) => normalizeExam(res.data));
}

export function remove(id) {
  return API.delete(`/${id}`);
}

// ====== ATTEMPTS ======
export function getExamAttemptsByUser(userId) {
  return API.get(`/user/${userId}`).then((res) => res.data);
}

export function getExamAttemptDetail(attemptId) {
  return API.get(`/attempt/${attemptId}`).then((res) => res.data);
}

export function getSubmittedDays(userId) {
  return getExamAttemptsByUser(userId).then((attempts) =>
    attempts
      .filter((a) => a.submittedAt)
      .map((a) => new Date(a.submittedAt).toISOString().split("T")[0])
  );
}
