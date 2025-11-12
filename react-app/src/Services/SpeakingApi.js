import axios from "axios";
import { getApiUrl } from "../config/api";

const API = axios.create({
  baseURL: getApiUrl("speaking"),
  withCredentials: true,
});

export function getAll() {
  return API.get("").then((res) => res.data);
}

export function getByExam(examId) {
  return API.get(`/exam/${examId}`).then((res) => res.data);
}

export function getById(id) {
  return API.get(`/${id}`).then((res) => res.data);
}

export function add(data) {
  return API.post("", data).then((res) => res.data);
}

export function update(id, data) {
  return API.put(`/${id}`, data).then((res) => res.data);
}

export function remove(id) {
  return API.delete(`/${id}`);
}

export const transcribeAudio = (attemptId, audioUrl) =>
  API.post("/transcribe", { attemptId, audioUrl })
    .then((r) => r.data)
    .catch((err) => {
      console.error(
        "Transcription API failed:",
        err.response?.data || err.message
      );
      return { transcript: "[Transcription failed]" };
    });

export const gradeSpeaking = (gradeData) =>
  API.post("/grade", gradeData)
    .then((r) => r.data)
    .catch((err) => {
      const msg = err.response?.data?.error || err.message;
      console.error("Grading API failed:", msg);
      alert(msg); // or use toast notification
      throw err;
    });
export async function getFeedbackBySpeakingId(speakingId, userId) {
  const res = await API.get(
    `/feedback/bySpeaking?speakingId=${speakingId}&userId=${userId}`
  );
  return res.data;
}

export const getFeedback = (examId, userId) =>
  API.get(`/feedback/${examId}/${userId}`).then((r) => r.data);
