import axios from 'axios';
import { getApiUrl } from '../config/api';

const api = axios.create({
  baseURL: getApiUrl('tag'),
  withCredentials: true,
});

// Tag API functions
export const getAllTags = async () => {
  try {
    const response = await api.get('');
    return response.data;
  } catch (error) {
    console.error('Error fetching tags:', error);
    throw error;
  }
};

export const getTagById = async (id) => {
  try {
    const response = await api.get(`/${id}`);
    return response.data;
  } catch (error) {
    console.error('Error fetching tag:', error);
    throw error;
  }
};

export const searchTags = async (query) => {
  try {
    const response = await api.get(`/search?query=${encodeURIComponent(query)}`);
    return response.data;
  } catch (error) {
    console.error('Error searching tags:', error);
    throw error;
  }
};

export const createTag = async (tagData) => {
  try {
    const response = await api.post('', tagData);
    return response.data;
  } catch (error) {
    console.error('Error creating tag:', error);
    throw error;
  }
};

export const updateTag = async (id, tagData) => {
  try {
    const response = await api.put(`/${id}`, tagData);
    return response.data;
  } catch (error) {
    console.error('Error updating tag:', error);
    throw error;
  }
};

export const deleteTag = async (id) => {
  try {
    const response = await api.delete(`/${id}`);
    return response.data;
  } catch (error) {
    console.error('Error deleting tag:', error);
    throw error;
  }
};

export default {
  getAllTags,
  getTagById,
  searchTags,
  createTag,
  updateTag,
  deleteTag,
};
