import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Search, X } from 'lucide-react';
import { getAllTags, createTag, updateTag, deleteTag } from '../../Services/TagApi';
import { getMe } from '../../Services/AuthApi';
import { useNavigate } from 'react-router-dom';
import './TagManagement.css';

export default function TagManagement() {
  const navigate = useNavigate();
  const [tags, setTags] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingTag, setEditingTag] = useState(null);
  const [tagName, setTagName] = useState('');
  const [error, setError] = useState('');
  const [user, setUser] = useState(null);

  useEffect(() => {
    checkAuth();
  }, []);

  const checkAuth = async () => {
    try {
      const response = await getMe();
      const userData = response.data;
      setUser(userData);
      
      console.log('User data:', userData);
      console.log('User roles:', userData.roles);
      
      // Check if user has moderator or admin role
      const userRole = userData.role || userData.roles;
      if (!userRole || (userRole !== 'moderator' && userRole !== 'admin')) {
        console.log('User does not have moderator/admin role, redirecting to dashboard');
        navigate('/dashboard');
        return;
      }
      
      console.log('User has moderator/admin role, loading tags...');
      loadTags();
    } catch (error) {
      console.error('Auth check failed:', error);
      navigate('/login');
    }
  };

  const loadTags = async () => {
    try {
      setLoading(true);
      const response = await getAllTags();
      setTags(response);
    } catch (error) {
      console.error('Error loading tags:', error);
      setError('Failed to load tags');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTag = async (e) => {
    e.preventDefault();
    if (!tagName.trim()) {
      setError('Tag name is required');
      return;
    }

    try {
      await createTag({ tagName: tagName.trim() });
      setTagName('');
      setShowModal(false);
      setError('');
      loadTags();
    } catch (error) {
      console.error('Error creating tag:', error);
      setError(error.response?.data?.message || 'Failed to create tag');
    }
  };

  const handleUpdateTag = async (e) => {
    e.preventDefault();
    if (!tagName.trim()) {
      setError('Tag name is required');
      return;
    }

    try {
      await updateTag(editingTag.tagId, { tagName: tagName.trim() });
      setTagName('');
      setEditingTag(null);
      setShowModal(false);
      setError('');
      loadTags();
    } catch (error) {
      console.error('Error updating tag:', error);
      setError(error.response?.data?.message || 'Failed to update tag');
    }
  };

  const handleDeleteTag = async (tagId) => {
    if (!window.confirm('Are you sure you want to delete this tag?')) {
      return;
    }

    try {
      await deleteTag(tagId);
      loadTags();
    } catch (error) {
      console.error('Error deleting tag:', error);
      setError(error.response?.data?.message || 'Failed to delete tag');
    }
  };

  const handleEditTag = (tag) => {
    setEditingTag(tag);
    setTagName(tag.tagName);
    setShowModal(true);
    setError('');
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setEditingTag(null);
    setTagName('');
    setError('');
  };

  const filteredTags = tags.filter(tag =>
    tag.tagName.toLowerCase().includes(searchQuery.toLowerCase())
  );

  if (loading || !user) {
    return (
      <div className="tag-management">
        <div className="loading">Loading...</div>
      </div>
    );
  }

  return (
    <div className="tag-management">
      <div className="tag-header">
        <h1>Tag Management</h1>
        <button 
          className="create-tag-btn"
          onClick={() => setShowModal(true)}
        >
          <Plus size={20} />
          Create Tag
        </button>
      </div>

      <div className="tag-search">
        <div className="search-input">
          <Search size={20} />
          <input
            type="text"
            placeholder="Search tags..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>
      </div>

      {error && (
        <div className="error-message">
          {error}
        </div>
      )}

      <div className="tags-table">
        <div className="table-header">
          <div className="col-name">Tag Name</div>
          <div className="col-posts">Posts</div>
          <div className="col-created">Created</div>
          <div className="col-actions">Actions</div>
        </div>

        <div className="table-body">
          {filteredTags.map(tag => (
            <div key={tag.tagId} className="table-row">
              <div className="col-name">
                <span className="tag-name">#{tag.tagName}</span>
              </div>
              <div className="col-posts">
                {tag.postCount}
              </div>
              <div className="col-created">
                {new Date(tag.createdAt).toLocaleDateString()}
              </div>
              <div className="col-actions">
                <button
                  className="edit-btn"
                  onClick={() => handleEditTag(tag)}
                >
                  <Edit size={16} />
                </button>
                <button
                  className="delete-btn"
                  onClick={() => handleDeleteTag(tag.tagId)}
                >
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
          ))}
        </div>

        {filteredTags.length === 0 && (
          <div className="no-tags">
            {searchQuery ? 'No tags found matching your search' : 'No tags available'}
          </div>
        )}
      </div>

      {showModal && (
        <div className="modal-overlay">
          <div className="modal">
            <div className="modal-header">
              <h2>{editingTag ? 'Edit Tag' : 'Create Tag'}</h2>
              <button className="close-btn" onClick={handleCloseModal}>
                <X size={20} />
              </button>
            </div>

            <form onSubmit={editingTag ? handleUpdateTag : handleCreateTag}>
              <div className="form-group">
                <label htmlFor="tagName">Tag Name</label>
                <input
                  type="text"
                  id="tagName"
                  value={tagName}
                  onChange={(e) => setTagName(e.target.value)}
                  placeholder="Enter tag name..."
                  required
                />
              </div>

              <div className="modal-actions">
                <button type="button" className="cancel-btn" onClick={handleCloseModal}>
                  Cancel
                </button>
                <button type="submit" className="submit-btn">
                  {editingTag ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
