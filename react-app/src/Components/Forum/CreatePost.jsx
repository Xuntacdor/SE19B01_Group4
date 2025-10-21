import React, { useState, useEffect } from 'react';
import { X, Plus, Tag } from 'lucide-react';
import { createPost } from '../../Services/ForumApi';
import { getAllTags } from '../../Services/TagApi';
import './CreatePost.css';

export default function CreatePost({ isOpen, onClose, onPostCreated }) {
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [selectedTags, setSelectedTags] = useState([]);
  const [availableTags, setAvailableTags] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (isOpen) {
      loadTags();
    }
  }, [isOpen]);

  const loadTags = async () => {
    try {
      const tags = await getAllTags();
      setAvailableTags(tags);
    } catch (error) {
      console.error('Error loading tags:', error);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!title.trim() || !content.trim()) {
      setError('Title and content are required');
      return;
    }

    setLoading(true);
    setError('');

    try {
      const postData = {
        title: title.trim(),
        content: content.trim(),
        tagNames: selectedTags.map(tag => tag.tagName)
      };

      const response = await createPost(postData);
      onPostCreated(response);
      
      // Reset form
      setTitle('');
      setContent('');
      setSelectedTags([]);
      onClose();
    } catch (error) {
      console.error('Error creating post:', error);
      setError(error.response?.data?.message || 'Failed to create post');
    } finally {
      setLoading(false);
    }
  };

  const handleTagSelect = (tag) => {
    if (!selectedTags.find(t => t.tagId === tag.tagId)) {
      setSelectedTags([...selectedTags, tag]);
    }
  };

  const handleTagRemove = (tagId) => {
    setSelectedTags(selectedTags.filter(t => t.tagId !== tagId));
  };

  const handleClose = () => {
    setTitle('');
    setContent('');
    setSelectedTags([]);
    setError('');
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay">
      <div className="create-post-modal">
        <div className="modal-header">
          <h2>Create New Post</h2>
          <button className="close-btn" onClick={handleClose}>
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="create-post-form">
          <div className="form-group">
            <label htmlFor="title">Title</label>
            <input
              type="text"
              id="title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Enter post title..."
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="content">Content</label>
            <textarea
              id="content"
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Write your post content..."
              rows={8}
              required
            />
          </div>

          <div className="form-group">
            <label>Tags (Optional)</label>
            <div className="tag-selection">
              <div className="selected-tags">
                {selectedTags.map(tag => (
                  <span key={tag.tagId} className="selected-tag">
                    #{tag.tagName}
                    <button
                      type="button"
                      className="remove-tag"
                      onClick={() => handleTagRemove(tag.tagId)}
                    >
                      <X size={14} />
                    </button>
                  </span>
                ))}
              </div>
              
              <div className="tag-dropdown">
                <div className="dropdown-header">
                  <Tag size={16} />
                  <span>Select tags</span>
                </div>
                <div className="dropdown-content">
                  {availableTags
                    .filter(tag => !selectedTags.find(t => t.tagId === tag.tagId))
                    .map(tag => (
                      <div
                        key={tag.tagId}
                        className="tag-option"
                        onClick={() => handleTagSelect(tag)}
                      >
                        #{tag.tagName}
                        <span className="tag-count">({tag.postCount})</span>
                      </div>
                    ))}
                </div>
              </div>
            </div>
          </div>

          {error && (
            <div className="error-message">
              {error}
            </div>
          )}

          <div className="modal-actions">
            <button type="button" className="cancel-btn" onClick={handleClose}>
              Cancel
            </button>
            <button type="submit" className="submit-btn" disabled={loading}>
              {loading ? 'Creating...' : 'Create Post'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
