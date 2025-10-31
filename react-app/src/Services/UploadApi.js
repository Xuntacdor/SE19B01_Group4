import axios from "axios";

const API = axios.create({
  baseURL: "https://localhost:7264/api/upload",
  withCredentials: true,
});

export function uploadImage(file) {
  const formData = new FormData();
  formData.append("file", file);

  return API.post("/image", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  }).then((res) => res.data);
}

export function uploadAudio(file) {
  const formData = new FormData();
  formData.append("file", file);

  return API.post("/audio", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  }).then((res) => res.data);
}

export function uploadDocument(file) {
  const formData = new FormData();
  formData.append("file", file);

  return API.post("/document", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  }).then((res) => res.data);
}

export function uploadFile(file) {
  const formData = new FormData();
  formData.append("file", file);

  return API.post("/file", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  }).then((res) => {
    console.log("Upload response:", res.data);
    return res.data;
  }).catch((error) => {
    console.error("Upload error:", error);
    throw error;
  });
}

// Utility functions for file validation
export function validateImageFile(file) {
  const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/bmp', 'image/webp', 'image/svg+xml'];
  const maxSize = 5 * 1024 * 1024; // 5MB

  if (!allowedTypes.includes(file.type)) {
    throw new Error('Please select a valid image file (JPG, PNG, GIF, BMP, WebP, SVG)');
  }

  if (file.size > maxSize) {
    throw new Error('Image size must be less than 5MB');
  }

  return true;
}

export function validateDocumentFile(file) {
  const allowedTypes = [
    'application/pdf',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    'text/plain',
    'application/rtf',
    'application/vnd.ms-excel',
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    'application/vnd.ms-powerpoint',
    'application/vnd.openxmlformats-officedocument.presentationml.presentation'
  ];
  const maxSize = 10 * 1024 * 1024; // 10MB

  if (!allowedTypes.includes(file.type)) {
    throw new Error('Please select a valid document file (PDF, DOC, DOCX, TXT, RTF, XLS, XLSX, PPT, PPTX)');
  }

  if (file.size > maxSize) {
    throw new Error('Document size must be less than 10MB');
  }

  return true;
}

export function getFileIcon(fileType) {
  const extension = fileType.toLowerCase();
  
  if (['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.svg'].includes(extension)) {
    return 'image';
  } else if (['.pdf'].includes(extension)) {
    return 'pdf';
  } else if (['.doc', '.docx'].includes(extension)) {
    return 'word';
  } else if (['.xls', '.xlsx'].includes(extension)) {
    return 'excel';
  } else if (['.ppt', '.pptx'].includes(extension)) {
    return 'powerpoint';
  } else if (['.txt', '.rtf'].includes(extension)) {
    return 'text';
  } else {
    return 'file';
  }
}

export function formatFileSize(bytes) {
  if (bytes === 0) return '0 Bytes';
  
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// Validate Cloudinary URL format
export function validateCloudinaryUrl(url) {
  console.log("🔍 Validating URL:", url);
  
  if (!url || typeof url !== 'string') {
    console.error("❌ Invalid URL (not a string):", url);
    return false;
  }

  // Check if URL starts with http
  if (!url.startsWith('http')) {
    console.error("❌ URL must start with http:", url);
    return false;
  }

  // Check if URL contains cloudinary domain
  if (!url.includes('res.cloudinary.com')) {
    console.error("❌ URL is not a Cloudinary URL:", url);
    return false;
  }

  // Check if URL has correct format with cloud name
  // Pattern: https://res.cloudinary.com/{cloud_name}/image/upload/{version}/... or /upload/{transformation}/...
  const cloudinaryPattern = /https:\/\/res\.cloudinary\.com\/[^/]+/;
  if (!cloudinaryPattern.test(url)) {
    console.error("❌ Cloudinary URL format is invalid:", url);
    console.log("Expected format: https://res.cloudinary.com/{cloud_name}/...");
    return false;
  }

  // Additional check: must have image/upload or raw/upload
  if (!url.includes('/upload/')) {
    console.error("❌ Cloudinary URL missing /upload/ path:", url);
    return false;
  }

  console.log("✅ Valid Cloudinary URL:", url);
  return true;
}
