// API Configuration
// In production, use the deployed backend URL
// In development, Vite proxy will handle /api requests

// Production backend URL
const PRODUCTION_API_BASE = 'https://webapi-823340438485.asia-southeast1.run.app';

// Helper to create full API URL
export const getApiUrl = (path) => {
  // Remove leading slash if present
  if (path.startsWith('/')) {
    path = path.substring(1);
  }
  
  // Check if we're in production (deployed on Firebase)
  if (import.meta.env.PROD) {
    // Production: use full URL with /api prefix
    return `${PRODUCTION_API_BASE}/api/${path}`;
  }
  
  // Development: use relative path (Vite proxy will handle it)
  return `/api/${path}`;
};

