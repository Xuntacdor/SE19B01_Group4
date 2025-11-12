import axios from "axios";
import { getApiUrl } from "../config/api"; 

const API = axios.create({
  baseURL: getApiUrl("upload"), 
  withCredentials: true,
});

export function uploadImage(file) {
  const formData = new FormData();
  formData.append("file", file);

  return API.post("/image", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  }).then((res) => res.data.url);
}

export function uploadAudio(file) {
  const formData = new FormData();
  formData.append("file", file);

  return API.post("/audio", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  }).then((res) => res.data.url);
}
