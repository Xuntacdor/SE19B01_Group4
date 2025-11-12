import React, { useState, useEffect } from "react";
import CommentItem from "./CommentItem";
import { getComments, createComment } from "../../Services/ForumApi";
import NotificationPopup from "./NotificationPopup";
import "./CommentSection.css";

export default function CommentSection({ postId, postOwnerId }) {
  const [comments, setComments] = useState([]);
  const [newComment, setNewComment] = useState("");
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [notification, setNotification] = useState({
    isOpen: false,
    type: "success",
    title: "",
    message: ""
  });

  const showNotification = (type, title, message) => {
    setNotification({ isOpen: true, type, title, message });
  };

  const closeNotification = () => {
    setNotification(prev => ({ ...prev, isOpen: false }));
  };

  useEffect(() => {
    loadComments();
    
    // Auto-refresh comments every 30 seconds to catch moderator deletions
    const refreshInterval = setInterval(() => {
      loadComments(false); // Don't show loading for background refreshes
    }, 30000);

    return () => clearInterval(refreshInterval);
  }, [postId]);

  const loadComments = (showLoading = true) => {
    if (showLoading) setLoading(true);
    getComments(postId)
      .then((response) => {
        console.log("Loaded comments:", response.data);
        setComments(response.data);
      })
      .catch((error) => {
        console.error("Error loading comments:", error);
      })
      .finally(() => {
        if (showLoading) setLoading(false);
      });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!newComment.trim() || submitting) return;

    setSubmitting(true);
    createComment(postId, newComment)
      .then((response) => {
        setComments((prev) => [response.data, ...prev]);
        setNewComment("");
        showNotification("success", "Comment Posted!", "Your comment has been added successfully.");
      })
      .catch((error) => {
        console.error("Error creating comment:", error);
        
        // Check if the error is due to account restriction
        const errorMessage = error.response?.data?.message || error.response?.data || error.message || "";
        
        if (errorMessage.toLowerCase().includes("restricted")) {
          showNotification(
            "error",
            "Account Restricted",
            "Your account has been restricted from commenting on the forum due to violations of community guidelines. Please contact support if you believe this is an error."
          );
        } else {
          showNotification(
            "error",
            "Error Creating Comment",
            "Unable to post your comment. Please try again later."
          );
        }
      })
      .finally(() => {
        setSubmitting(false);
      });
  };

  const handleCommentCreated = (newComment) => {
    console.log("New comment created:", newComment);
    console.log("Has parentCommentId:", newComment.parentCommentId);
    
    // Xử lý delete comment
    if (newComment.action === 'delete') {
      setComments((prev) => {
        const removeComment = (comments) => {
          return comments.filter(comment => comment.commentId !== newComment.commentId)
            .map(comment => ({
              ...comment,
              replies: comment.replies ? removeComment(comment.replies) : []
            }));
        };
        return removeComment(prev);
      });
      return;
    }
    
    // Nếu là reply, cần cập nhật comment cha (có thể ở bất kỳ tầng nào)
    if (newComment.parentCommentId) {
      setComments((prev) => {
        const updateCommentWithReply = (comments) => {
          return comments.map(comment => {
            if (comment.commentId === newComment.parentCommentId) {
              console.log("Updating parent comment:", comment.commentId);
              return { ...comment, replies: [...comment.replies, newComment] };
            }
            // Recursively update nested comments
            if (comment.replies && comment.replies.length > 0) {
              return { ...comment, replies: updateCommentWithReply(comment.replies) };
            }
            return comment;
          });
        };
        
        const updated = updateCommentWithReply(prev);
        console.log("Updated comments:", updated);
        return updated;
      });
    } else {
      // Nếu là comment gốc, thêm vào đầu danh sách
      setComments((prev) => [newComment, ...prev]);
    }
  };

  if (loading) {
    return (
      <div className="comment-section">
        <div className="loading">Loading comments...</div>
      </div>
    );
  }

  return (
    <div className="comment-section">
      <div className="comment-form">
        <form onSubmit={handleSubmit}>
          <textarea
            value={newComment}
            onChange={(e) => setNewComment(e.target.value)}
            placeholder="Type here your wise suggestion"
            rows={5}
            required
          />
          <div className="comment-actions">
            <button
              type="submit"
              className="forum-comment-btn forum-comment-btn-primary"
              disabled={submitting || !newComment.trim()}
            >
              {submitting ? "Suggesting..." : "Suggest"}
            </button>
          </div>
        </form>
      </div>

      <div className="comments-list">
        {comments.length === 0 ? (
          <div className="no-comments">
            No comments yet. Be the first to comment!
          </div>
        ) : (
          comments
            .filter(comment => !comment.parentCommentId) // Chỉ hiển thị comments gốc
            .map((comment) => (
              <CommentItem
                key={comment.commentId}
                comment={comment}
                onReply={handleCommentCreated}
                level={0}
                postId={postId}
                postOwnerId={postOwnerId}
              />
            ))
        )}
      </div>

      <NotificationPopup
        isOpen={notification.isOpen}
        onClose={closeNotification}
        type={notification.type}
        title={notification.title}
        message={notification.message}
      />
    </div>
  );
}
