import axios from 'axios';
import { getApiUrl } from './api';

// Create a base axios instance with common configuration
export const createApiClient = (endpoint) => {
  return axios.create({
    baseURL: getApiUrl(endpoint),
    withCredentials: true,
  });
};

// Default API client (for general use)
export const apiClient = createApiClient('');

