import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import "./CreatePost.css";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import HeaderBar from "../../Components/Layout/HeaderBar";
import RightSidebar from "../../Components/Forum/RightSidebar";
import { createPost } from "../../Services/ForumApi";
import { getAllTags } from "../../Services/TagApi";
import { uploadFile, validateImageFile, validateDocumentFile, getFileIcon, formatFileSize, validateCloudinaryUrl } from "../../Services/UploadApi";
import { Image, Send, X, Upload, FileText, File, Trash2, Tag, ImagePlus } from "lucide-react";
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
        console.log("Upload response for file:", file.name, response);
        
        const uploadedFile = {
          id: Date.now() + Math.random(),
          fileName: file.name,
          fileSize: file.size,
          fileType: response.category || (isImage ? 'image' : 'document'),
          fileExtension: fileExtension,
          url: response.url,
        };
        
        console.log("Created uploaded file object:", uploadedFile);
        return uploadedFile;
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
        
        // Check if the error is due to account restriction
        const errorMessage = error.response?.data?.message || error.response?.data || error.message || "";
        
        if (errorMessage.toLowerCase().includes("restricted")) {
          showNotification(
            "error",
            "Account Restricted",
            "Your account has been restricted from posting on the forum due to violations of community guidelines. Please contact support if you believe this is an error."
          );
        } else {
          showNotification(
            "error",
            "Error Creating Post",
            errorMessage || "Unable to create your post. Please try again later."
          );
        }
      })
      .finally(() => {
        setLoading(false);
      });
  };

  const handleAddImage = () => {
    document.getElementById('file-upload').click();
  };

  const insertImageAtCursor = (imageUrl, fileName) => {
    const textarea = document.getElementById('content');
    if (!textarea) return;

    console.log("Inserting image:", fileName, "URL:", imageUrl);

    // Validate URL format
    if (!validateCloudinaryUrl(imageUrl)) {
      showNotification(
        "error",
        "Invalid URL",
        "Invalid image URL. The URL must be a valid Cloudinary URL. Please upload the image again."
      );
      return;
    }

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const text = formData.content;
    const before = text.substring(0, start);
    const after = text.substring(end);
    
    // Insert markdown image syntax
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

              {/* Tag Selection Section - Moved below title */}
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
                          <div className="file-actions">
                            {file.fileType === 'image' && (
                              <button
                                type="button"
                                className="btn btn-insert-image"
                                onClick={() => handleInsertImage(file.url, file.fileName)}
                                data-tooltip="Insert image at cursor position in content area"
                              >
                                <ImagePlus size={14} />
                              </button>
                            )}
                            <button
                              type="button"
                              className="btn btn-remove-file"
                              onClick={() => handleRemoveFile(file.id)}
                            >
                              <Trash2 size={14} />
                            </button>
                          </div>
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