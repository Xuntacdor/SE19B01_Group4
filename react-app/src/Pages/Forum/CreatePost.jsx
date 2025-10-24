import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import "./CreatePost.css";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import HeaderBar from "../../Components/Layout/HeaderBar";
import RightSidebar from "../../Components/Forum/RightSidebar";
import { createPost } from "../../Services/ForumApi";
import { getAllTags } from "../../Services/TagApi";
import { uploadFile, validateImageFile, validateDocumentFile, getFileIcon, formatFileSize } from "../../Services/UploadApi";
import { Image, Send, X, Upload, FileText, File, Trash2, Tag } from "lucide-react";
import NotificationPopup from "../../Components/Forum/NotificationPopup";

export default function CreatePost() {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    title: "",
    content: "",
    tagNames: []
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [uploadedFiles, setUploadedFiles] = useState([]);
  const [uploading, setUploading] = useState(false);
  const [selectedTags, setSelectedTags] = useState([]);
  const [availableTags, setAvailableTags] = useState([]);
  const [notification, setNotification] = useState({
    isOpen: false,
    type: "success",
    title: "",
    message: ""
  });

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value
    }));
  };

  const showNotification = (type, title, message) => {
    setNotification({
      isOpen: true,
      type,
      title,
      message
    });
  };

  const closeNotification = () => {
    setNotification(prev => ({ ...prev, isOpen: false }));
    // Navigate to forum only if it was a success notification
    if (notification.type === "success") {
      navigate("/forum");
    }
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
      showNotification(
        "error",
        "File upload error",
        errorMessage
      );
    } finally {
      setUploading(false);
    }
  };

  const handleRemoveFile = (fileId) => {
    setUploadedFiles(prev => prev.filter(file => file.id !== fileId));
  };

  const loadTags = async () => {
    try {
      const tags = await getAllTags();
      setAvailableTags(tags);
    } catch (error) {
      console.error('Error loading tags:', error);
    }
  };

  useEffect(() => {
    loadTags();
  }, []);

  const handleTagSelect = (tag) => {
    if (!selectedTags.find(t => t.tagId === tag.tagId)) {
      setSelectedTags([...selectedTags, tag]);
    }
  };

  const handleTagRemove = (tagId) => {
    setSelectedTags(selectedTags.filter(t => t.tagId !== tagId));
  };

  const getFileIconComponent = (fileType) => {
    const iconType = getFileIcon(`.${fileType}`);
    switch (iconType) {
      case 'image':
        return <Image size={16} />;
      case 'pdf':
        return <FileText size={16} />;
      case 'word':
        return <FileText size={16} />;
      case 'excel':
        return <FileText size={16} />;
      case 'powerpoint':
        return <FileText size={16} />;
      case 'text':
        return <FileText size={16} />;
      default:
        return <File size={16} />;
    }
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!formData.title.trim() || !formData.content.trim()) {
      const errorMessage = "Please fill in all required fields";
      setError(errorMessage);
      showNotification(
        "warning",
        "Missing information",
        errorMessage
      );
      return;
    }

    setLoading(true);
    setError("");

    const postData = {
      title: formData.title.trim(),
      content: formData.content.trim(),
      tagNames: selectedTags.map(tag => tag.tagName),
      attachments: uploadedFiles.map(file => ({
        fileName: file.fileName,
        fileUrl: file.url,
        fileType: file.fileType,
        fileExtension: file.fileExtension,
        fileSize: file.fileSize
      }))
    };

    createPost(postData)
      .then((response) => {
        showNotification(
          "success",
          "Post created successfully!",
          "Your post has been created successfully and is awaiting approval. You will be notified when the post is approved."
        );
      })
      .catch((error) => {
        console.error("Error creating post:", error);
        showNotification(
          "error",
          "Error creating post",
          error.response?.data?.message || "Error creating post. Please try again."
        );
      })
      .finally(() => {
        setLoading(false);
      });
  };

  const handleSaveDraft = () => {
    // TODO: Implement save as draft functionality
    console.log("Save as draft:", formData);
  };

  const handleAddImage = () => {
    document.getElementById('file-upload').click();
  };

  return (
    <div className="create-post-container">
      <GeneralSidebar />
      
      <main className="main-content">
        <HeaderBar currentPage="createPost" />
        
        <div className="create-post-content">
          <div className="create-post-main">
            <div className="create-post-header">
              <h1>New Post</h1>
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

              {/* Tag Selection Section */}
              <div className="form-group">
                <label htmlFor="tags">Tags (Optional)</label>
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
                            <span className="tag-count">({tag.postCount || 0})</span>
                          </div>
                        ))}
                    </div>
                  </div>
                </div>
              </div>

              {/* File Upload Section */}
              <div className="form-group">
                <label htmlFor="files">Attachments (Optional)</label>
                <div className="file-upload-section">
                  <input
                    type="file"
                    id="file-upload"
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
                    <p className="upload-hint">
                      Supported: Images (JPG, PNG, GIF, BMP, WebP, SVG), Documents (PDF, DOC, DOCX, TXT, RTF, XLS, XLSX, PPT, PPTX)
                    </p>
                    <p className="upload-hint">
                      Max size: Images 5MB, Documents 10MB
                    </p>
                  </div>

                  {uploadedFiles.length > 0 && (
                    <div className="uploaded-files">
                      <h4>Uploaded Files:</h4>
                      {uploadedFiles.map(file => (
                        <div key={file.id} className="uploaded-file-item">
                          <div className="file-info">
                            {getFileIconComponent(file.fileType)}
                            <div className="file-details">
                              <span className="file-name">{file.fileName}</span>
                              <span className="file-size">{formatFileSize(file.fileSize)}</span>
                            </div>
                          </div>
                          <button
                            type="button"
                            className="btn btn-remove-file"
                            onClick={() => handleRemoveFile(file.id)}
                          >
                            <Trash2 size={14} />
                          </button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              {error && (
                <div className="error-message">
                  {error}
                </div>
              )}

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
                    {loading ? "Publishing..." : "Publish"}
                  </button>
                </div>
                <button 
                  type="button" 
                  className="btn btn-cancel"
                  onClick={() => navigate("/forum")}
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

      <NotificationPopup
        isOpen={notification.isOpen}
        onClose={closeNotification}
        type={notification.type}
        title={notification.title}
        message={notification.message}
        duration={0}
      />
    </div>
  );
}