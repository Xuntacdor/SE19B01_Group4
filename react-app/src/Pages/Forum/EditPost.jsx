import React, { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import "./CreatePost.css";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import HeaderBar from "../../Components/Layout/HeaderBar";
import RightSidebar from "../../Components/Forum/RightSidebar";
import { getPost, updatePost } from "../../Services/ForumApi";
import { getAllTags } from "../../Services/TagApi";
import { uploadFile, validateImageFile, validateDocumentFile, getFileIcon, formatFileSize, validateCloudinaryUrl } from "../../Services/UploadApi";
import { Image, Send, X, Tag, Upload, Trash2, ImagePlus } from "lucide-react";
import { marked } from "marked";

export default function EditPost({ onNavigate }) {
  const { postId } = useParams();
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    title: "",
    content: "",
    tagNames: []
  });
  const [selectedTags, setSelectedTags] = useState([]);
  const [availableTags, setAvailableTags] = useState([]);
  const [loading, setLoading] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [uploadedFiles, setUploadedFiles] = useState([]);
  const [existingAttachments, setExistingAttachments] = useState([]);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    loadPost();
    loadTags();
  }, [postId]);

  const loadPost = () => {
    getPost(postId, false) // Không tăng view count khi edit
      .then((response) => {
        const post = response.data;
        setFormData({
          title: post.title || "",
          content: post.content || "",
          tagNames: post.tags ? post.tags.map(tag => tag.tagName) : []
        });
        // Set selected tags for UI
        setSelectedTags(post.tags || []);
        // Load existing attachments separately
        if (post.attachments && post.attachments.length > 0) {
        console.log("Post attachments:", post.attachments);
        setExistingAttachments(post.attachments.map(att => {
          console.log("Attachment fileUrl:", att.fileUrl);
          return {
            id: att.attachmentId,
            fileName: att.fileName,
            fileUrl: att.fileUrl,  // Keep original property name
            url: att.fileUrl,  // Also add url for compatibility
            fileType: att.fileType,
            fileExtension: att.fileExtension,
            fileSize: att.fileSize
          };
        }));
        }
      })
      .catch((error) => {
        console.error("Error loading post:", error);
        alert("Error loading post. Please try again.");
        navigate("/forum");
      })
      .finally(() => {
        setInitialLoading(false);
      });
  };

  const loadTags = async () => {
    try {
      const tags = await getAllTags();
      setAvailableTags(tags);
    } catch (error) {
      console.error('Error loading tags:', error);
    }
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!formData.title.trim() || !formData.content.trim()) {
      alert("Please fill in all required fields");
      return;
    }

    setLoading(true);
    setError("");

    // Combine existing attachments with newly uploaded files
    const allAttachments = [
      ...existingAttachments,
      ...uploadedFiles
    ];

    const postData = {
      title: formData.title.trim(),
      content: formData.content.trim(),
      tagNames: selectedTags.map(tag => tag.tagName),
      attachments: allAttachments.map(file => ({
        fileName: file.fileName,
        fileUrl: file.url || file.fileUrl,  // Support both property names
        fileType: file.fileType,
        fileExtension: file.fileExtension,
        fileSize: file.fileSize
      }))
    };
    
    console.log("Updating post with data:", postData);
    console.log("Existing attachments:", existingAttachments.length);
    console.log("New uploaded files:", uploadedFiles.length);
    console.log("Total attachments:", allAttachments.length);
    console.log("All attachments:", allAttachments);
    
    updatePost(postId, postData)
      .then((response) => {
        console.log("Post updated successfully, navigating...");
        // Force reload page to get fresh data
        window.location.href = `/post/${postId}`;
      })
      .catch((error) => {
        console.error("Error updating post:", error);
        console.error("Error details:", error.response?.data);
        setError(error.response?.data?.message || "Error updating post. Please try again.");
        alert(error.response?.data?.message || "Error updating post. Please try again.");
      })
      .finally(() => {
        setLoading(false);
      });
  };

  const handleSaveDraft = () => {
    // TODO: Implement save as draft functionality
    console.log("Save as draft:", formData);
  };

  const handleFileUpload = async (e) => {
    const files = Array.from(e.target.files);
    if (files.length === 0) return;

    setUploading(true);
    setError("");

    try {
      const uploadPromises = files.map(async (file) => {
        // Validate file based on type
        const fileExtension = file.name.split('.').pop().toLowerCase();
        const isImage = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'svg'].includes(fileExtension);
        const isDocument = ['pdf', 'doc', 'docx', 'txt', 'rtf', 'xls', 'xlsx', 'ppt', 'pptx'].includes(fileExtension);

        if (isImage) {
          validateImageFile(file);
        } else if (isDocument) {
          validateDocumentFile(file);
        } else {
          throw new Error(`File type .${fileExtension} is not supported`);
        }

        const response = await uploadFile(file);
        return {
          id: Date.now() + Math.random(),
          fileName: file.name,
          fileSize: file.size,
          fileType: response.category || (isImage ? 'image' : 'document'),
          fileExtension: fileExtension,
          url: response.url,
        };
      });

      const uploadedFiles = await Promise.all(uploadPromises);
      setUploadedFiles(prev => [...prev, ...uploadedFiles]);
    } catch (error) {
      console.error('Error uploading files:', error);
      const errorMessage = error.message || 'Failed to upload files. Please try again.';
      setError(errorMessage);
      alert(errorMessage);
    } finally {
      setUploading(false);
    }
  };

  const handleRemoveFile = (fileId, isExisting) => {
    if (isExisting) {
      setExistingAttachments(prev => prev.filter(file => file.id.toString() !== fileId.toString()));
    } else {
      setUploadedFiles(prev => prev.filter(file => file.id.toString() !== fileId.toString()));
    }
  };

  const handleAddImage = () => {
    document.getElementById('file-upload-edit').click();
  };

  const insertImageAtCursor = (imageUrl, fileName) => {
    const textarea = document.getElementById('content');
    if (!textarea) return;

    console.log("Inserting image:", fileName, "URL:", imageUrl);

    // Validate URL format
    if (!validateCloudinaryUrl(imageUrl)) {
      alert("Invalid image URL. The URL must be a valid Cloudinary URL. Please upload the image again.");
      return;
    }

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const text = formData.content;
    const before = text.substring(0, start);
    const after = text.substring(end);
    
    // Insert markdown image syntax with full URL
    const imageMarkdown = `\n\n![${fileName}](${imageUrl})\n\n`;
    const newContent = before + imageMarkdown + after;
    
    console.log("✅ Successfully inserted image:", imageUrl);
    
    setFormData(prev => ({
      ...prev,
      content: newContent
    }));

    // Move cursor to after the inserted image
    setTimeout(() => {
      textarea.focus();
      textarea.setSelectionRange(start + imageMarkdown.length, start + imageMarkdown.length);
    }, 0);
  };

  const handleInsertImage = (imageUrl, fileName) => {
    insertImageAtCursor(imageUrl, fileName);
  };

  const handleTagSelect = (tag) => {
    if (!selectedTags.find(t => t.tagId === tag.tagId)) {
      setSelectedTags([...selectedTags, tag]);
    }
  };

  const handleTagRemove = (tagId) => {
    setSelectedTags(selectedTags.filter(t => t.tagId !== tagId));
  };

  if (initialLoading) {
    return (
      <div className="create-post-container">
        <GeneralSidebar />
        <main className="main-content">
          <HeaderBar onNavigate={onNavigate} currentPage="editPost" />
          <div className="create-post-content">
            <div className="loading">Loading post...</div>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="create-post-container">
      <GeneralSidebar />
      
      <main className="main-content">
        <HeaderBar onNavigate={onNavigate} currentPage="editPost" />
        
        <div className="create-post-content">
          <div className="create-post-main">
            <div className="create-post-header">
              <h1>Edit Post</h1>
            </div>
            
            <form onSubmit={handleSubmit} className="create-post-form">
              <div className="form-group">
                <input
                  type="text"
                  id="title"
                  name="title"
                  value={formData.title}
                  onChange={handleInputChange}
                  placeholder="Type catching attention title"
                  required
                  className="form-input"
                />
              </div>

              {/* Tag Selection Section - Moved below title */}
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

              <div className="form-group">
                <textarea
                  id="content"
                  name="content"
                  value={formData.content}
                  onChange={handleInputChange}
                  placeholder="Type whatever you want to describe"
                  rows={12}
                  required
                  className="form-textarea"
                />
              </div>

              {/* File Upload Section */}
              <div className="form-group">
                <label>Attachments</label>
                <div className="file-upload-section">
                  <input
                    type="file"
                    id="file-upload-edit"
                    accept="image/*,.pdf,.doc,.docx,.txt,.rtf,.xls,.xlsx,.ppt,.pptx"
                    onChange={handleFileUpload}
                    multiple
                    style={{ display: 'none' }}
                  />
                  
                  <div className="file-upload-area">
                    <button
                      type="button"
                      className="btn btn-file-upload"
                      onClick={handleAddImage}
                      disabled={uploading}
                    >
                      <Upload size={16} />
                      {uploading ? "Uploading..." : "Upload Files"}
                    </button>
                  </div>

                  {(existingAttachments.length > 0 || uploadedFiles.length > 0) && (
                    <div className="uploaded-files">
                      <h4>Files:</h4>
                      
                      {/* Existing attachments */}
                      {existingAttachments.map(file => (
                        <div key={`existing-${file.id}`} className="uploaded-file-item">
                          <div className="file-info">
                            {file.fileType === 'image' ? (
                              <Image size={16} />
                            ) : (
                              <Tag size={16} />
                            )}
                            <div className="file-details">
                              <span className="file-name">{file.fileName}</span>
                              <span className="file-size">{formatFileSize(file.fileSize)}</span>
                            </div>
                          </div>
                          <div className="file-actions">
                            {file.fileType === 'image' && (
                              <button
                                type="button"
                                className="btn btn-insert-image"
                                onClick={() => handleInsertImage(file.url, file.fileName)}
                                title="Insert into content"
                              >
                                <ImagePlus size={14} />
                              </button>
                            )}
                            <button
                              type="button"
                              className="btn btn-remove-file"
                              onClick={() => handleRemoveFile(file.id, true)}
                            >
                              <Trash2 size={14} />
                            </button>
                          </div>
                        </div>
                      ))}
                      
                      {/* Newly uploaded files */}
                      {uploadedFiles.map(file => (
                        <div key={`new-${file.id}`} className="uploaded-file-item">
                          <div className="file-info">
                            {file.fileType === 'image' ? (
                              <Image size={16} />
                            ) : (
                              <Tag size={16} />
                            )}
                            <div className="file-details">
                              <span className="file-name">{file.fileName}</span>
                              <span className="file-size">{formatFileSize(file.fileSize)}</span>
                            </div>
                          </div>
                          <div className="file-actions">
                            {file.fileType === 'image' && (
                              <button
                                type="button"
                                className="btn btn-insert-image"
                                onClick={() => handleInsertImage(file.url, file.fileName)}
                                title="Insert into content"
                              >
                                <ImagePlus size={14} />
                              </button>
                            )}
                            <button
                              type="button"
                              className="btn btn-remove-file"
                              onClick={() => handleRemoveFile(file.id, false)}
                            >
                              <Trash2 size={14} />
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}

                  {error && (
                    <div className="error-message">
                      {error}
                    </div>
                  )}
                </div>
              </div>

              <div className="form-actions">
                <div className="form-actions-left">
                  <button 
                    type="button" 
                    className="btn btn-image"
                    onClick={handleAddImage}
                  >
                    <Image size={16} />
                    Add Image
                  </button>
                  <button 
                    type="button" 
                    className="btn btn-draft"
                    onClick={handleSaveDraft}
                  >
                    Save as draft
                  </button>
                  <button 
                    type="submit" 
                    className="btn btn-publish"
                    disabled={loading}
                  >
                    <Send size={16} />
                    {loading ? "Updating..." : "Update Post"}
                  </button>
                </div>
                <button 
                  type="button" 
                  className="btn btn-cancel"
                  onClick={() => navigate(`/post/${postId}`)}
                >
                  <X size={16} />
                  Cancel
                </button>
              </div>
            </form>
          </div>
          
          <RightSidebar />
        </div>
      </main>
    </div>
  );
}



