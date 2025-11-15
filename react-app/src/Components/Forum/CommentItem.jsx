import React, { useState, useEffect, useRef } from "react";
import { likeComment, unlikeComment, createComment, reportComment, updateComment, deleteComment } from "../../Services/ForumApi";
import { ThumbsUp, MoreVertical, Flag, Edit, Trash2, X } from "lucide-react";
import useAuth from "../../Hook/UseAuth";
import { formatTimeVietnam } from "../../utils/date";
import NotificationPopup from "../Forum/NotificationPopup";

export default function CommentItem({ comment, onReply, level = 0, postId, postOwnerId }) {
  const { user } = useAuth();
  
  const [isVoted, setIsVoted] = useState(comment.isVoted || false);
  const [voteCount, setVoteCount] = useState(comment.voteCount || comment.likeNumber || 0);
  const [showReplyForm, setShowReplyForm] = useState(false);
  const [replyText, setReplyText] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [showReplies, setShowReplies] = useState(false); // Đóng replies mặc định
  const [showMenu, setShowMenu] = useState(false);
  const [showReportModal, setShowReportModal] = useState(false);
  const [showEditForm, setShowEditForm] = useState(false);
  const [reportReason, setReportReason] = useState("");
  const [editContent, setEditContent] = useState(comment.content);
  const [isSubmittingReport, setIsSubmittingReport] = useState(false);
  const [isSubmittingEdit, setIsSubmittingEdit] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const menuRef = useRef(null);
  
  // Notification state
  const [notification, setNotification] = useState({
    isOpen: false,
    type: "success",
    title: "",
    message: ""
  });

  // Sync state with props when component mounts or props change
  useEffect(() => {
    setIsVoted(comment.isVoted || false);
    setVoteCount(comment.voteCount || comment.likeNumber || 0);
    setEditContent(comment.content);
  }, [comment.isVoted, comment.voteCount, comment.likeNumber, comment.content]);

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (menuRef.current && !menuRef.current.contains(event.target)) {
        setShowMenu(false);
      }
    };

    if (showMenu) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [showMenu]);


  // Notification functions
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
  };

  const handleVote = (e) => {
    e.stopPropagation();
    
    if (isVoted) {
      unlikeComment(comment.commentId)
        .then(() => {
          setIsVoted(false);
          setVoteCount(prev => prev - 1);
        })
        .catch(error => {
          console.error("Error unliking comment:", error);
        });
    } else {
      likeComment(comment.commentId)
        .then(() => {
          setIsVoted(true);
          setVoteCount(prev => prev + 1);
        })
        .catch(error => {
          console.error("Error liking comment:", error);
        });
    }
  };


  const handleReply = (e) => {
    e.preventDefault();
    if (!replyText.trim() || submitting) return;

    setSubmitting(true);
    createComment(postId, replyText, comment.commentId)
      .then((response) => {
        // Đảm bảo reply có parentCommentId đúng
        const replyData = {
          ...response.data,
          parentCommentId: comment.commentId
        };
        onReply(replyData);
        setReplyText("");
        setShowReplyForm(false);
        showNotification("success", "Reply Posted!", "Your reply has been added successfully.");
      })
      .catch(error => {
        console.error("Error creating reply:", error);
        
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
            "Error Creating Reply",
            "Unable to post your reply. Please try again later."
          );
        }
      })
      .finally(() => {
        setSubmitting(false);
      });
  };

  const formatTime = (dateString) => {
    return formatTimeVietnam(dateString);
  };

  // Check permissions
  const isOwner = user && comment.user?.userId === user.userId;
  const isAdmin = user && user.role === 'admin';
  const isPostOwner = user && postOwnerId === user.userId;
  const canEdit = isOwner;
  const canDelete = isOwner || isAdmin || isPostOwner;
  const canReport = user && !isOwner;

  // Handle menu toggle
  const handleMenuToggle = (e) => {
    e.stopPropagation();
    setShowMenu(!showMenu);
  };

  // Handle report
  const handleReport = () => {
    setShowMenu(false);
    setShowReportModal(true);
  };

  const handleSubmitReport = async (e) => {
    e.preventDefault();
    if (!reportReason.trim() || isSubmittingReport) return;

    setIsSubmittingReport(true);
    try {
      await reportComment(comment.commentId, reportReason.trim());
      setShowReportModal(false);
      setReportReason("");
      showNotification("success", "Comment Reported", "Thank you for your report. We will review it soon.");
    } catch (error) {
      console.error("Error reporting comment:", error);
      const errorMessage = error.response?.data?.message || error.response?.data || error.message || "Failed to report comment";
      showNotification("error", "Report Failed", errorMessage);
    } finally {
      setIsSubmittingReport(false);
    }
  };

  // Handle edit
  const handleEdit = () => {
    setShowMenu(false);
    setShowEditForm(true);
    setEditContent(comment.content);
  };

  const handleCancelEdit = () => {
    setShowEditForm(false);
    setEditContent(comment.content);
  };

  const handleSubmitEdit = async (e) => {
    e.preventDefault();
    if (!editContent.trim() || editContent === comment.content || isSubmittingEdit) return;

    setIsSubmittingEdit(true);
    try {
      await updateComment(comment.commentId, editContent.trim());
      // Update local comment content
      comment.content = editContent.trim();
      setShowEditForm(false);
      showNotification("success", "Comment Updated", "Your comment has been updated successfully.");
    } catch (error) {
      console.error("Error updating comment:", error);
      const errorMessage = error.response?.data?.message || error.response?.data || error.message || "Failed to update comment";
      showNotification("error", "Update Failed", errorMessage);
    } finally {
      setIsSubmittingEdit(false);
    }
  };

  // Handle delete
  const handleDelete = async () => {
    if (!window.confirm("Are you sure you want to delete this comment?")) {
      setShowMenu(false);
      return;
    }

    setIsDeleting(true);
    try {
      await deleteComment(postId, comment.commentId);
      // Notify parent to remove comment
      onReply({ action: 'delete', commentId: comment.commentId });
      showNotification("success", "Comment Deleted", "The comment has been deleted successfully.");
    } catch (error) {
      console.error("Error deleting comment:", error);
      const errorMessage = error.response?.data?.message || error.response?.data || error.message || "Failed to delete comment";
      showNotification("error", "Delete Failed", errorMessage);
    } finally {
      setIsDeleting(false);
      setShowMenu(false);
    }
  };

  const hasReplies = comment.replies && comment.replies.length > 0;
  
  // Đếm tất cả nested comments (bao gồm replies của replies)
  const countAllNestedComments = (replies) => {
    if (!replies || replies.length === 0) return 0;
    
    let total = replies.length;
    replies.forEach(reply => {
      total += countAllNestedComments(reply.replies);
    });
    return total;
  };
  
  const replyCount = comment.replies ? countAllNestedComments(comment.replies) : 0;
  
  // Debug log để xem cấu trúc comment
  console.log('Comment data:', {
    commentId: comment.commentId,
    content: comment.content,
    isVoted: comment.isVoted,
    voteCount: comment.voteCount,
    likeNumber: comment.likeNumber,
    replies: comment.replies,
    replyCount: replyCount,
    hasReplies: hasReplies
  });

  return (
    <div className={`comment-item ${level > 0 ? 'reply-item' : ''}`}>
      <div className="comment-header">
        <img
          src={comment.user?.avatar || "/default-avatar.png"}
          alt={comment.user?.username}
          className="comment-avatar"
        />
        <div className="comment-user-info">
          <div className="comment-user-header">
            <span className="comment-username">@{comment.user?.username}</span>
            {(canEdit || canDelete || canReport) && (
              <div className="comment-menu" ref={menuRef}>
                <button 
                  className="comment-menu-btn" 
                  onClick={handleMenuToggle}
                  aria-label="Comment options"
                >
                  <MoreVertical size={16} />
                </button>
                {showMenu && (
                  <div className="comment-menu-dropdown">
                    {canReport && (
                      <button 
                        className="forum-comment-menu-item report-item"
                        onClick={handleReport}
                      >
                        <Flag size={16} />
                        Report
                      </button>
                    )}
                    {canEdit && (
                      <button 
                        className="forum-comment-menu-item"
                        onClick={handleEdit}
                      >
                        <Edit size={16} />
                        Edit
                      </button>
                    )}
                    {canDelete && (
                      <button 
                        className="forum-comment-menu-item delete-item"
                        onClick={handleDelete}
                        disabled={isDeleting}
                      >
                        <Trash2 size={16} />
                        {isDeleting ? "Deleting..." : "Delete"}
                      </button>
                    )}
                  </div>
                )}
              </div>
            )}
          </div>
          {showEditForm ? (
            <form className="edit-comment-form" onSubmit={handleSubmitEdit}>
              <textarea
                value={editContent}
                onChange={(e) => setEditContent(e.target.value)}
                className="forum-comment-edit-textarea"
                rows={3}
                required
              />
              <div className="edit-actions">
                <button
                  type="button"
                  className="forum-comment-btn forum-comment-btn-secondary"
                  onClick={handleCancelEdit}
                  disabled={isSubmittingEdit}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="forum-comment-btn forum-comment-btn-primary"
                  disabled={isSubmittingEdit || !editContent.trim() || editContent === comment.content}
                >
                  {isSubmittingEdit ? "Saving..." : "Save"}
                </button>
              </div>
            </form>
          ) : (
            <div className="comment-content">
              {comment.content}
            </div>
          )}
        </div>
      </div>

      <div className="comment-actions">
        <div className="comment-actions-left">
          <span className="comment-time">{formatTime(comment.createdAt)}</span>
          
          <button
            className="comment-action-btn"
            onClick={() => setShowReplyForm(!showReplyForm)}
          >
            Reply
          </button>
          
          <button
            className={`comment-action-btn vote-btn ${isVoted ? "voted" : ""}`}
            onClick={handleVote}
          >
            <ThumbsUp size={16} />
            <span>{isVoted ? "Liked" : "Like"}</span>
            <span className="vote-count">({voteCount})</span>
          </button>
        </div>
        
        <div className="comment-actions-right">
          {hasReplies && (
            <button
              className="comment-action-btn toggle-replies-btn"
              onClick={() => setShowReplies(!showReplies)}
            >
              {showReplies ? `Hide ${replyCount} replies` : `View all ${replyCount} replies`}
            </button>
          )}
        </div>
      </div>

      {showReplyForm && (
        <form className="reply-form" onSubmit={handleReply}>
          <textarea
            value={replyText}
            onChange={(e) => setReplyText(e.target.value)}
            placeholder="Type your reply..."
            rows={4}
            required
          />
          <div className="reply-actions">
            <button
              type="button"
              className="forum-comment-btn forum-comment-btn-secondary"
              onClick={() => setShowReplyForm(false)}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="forum-comment-btn forum-comment-btn-primary"
              disabled={submitting || !replyText.trim()}
            >
              {submitting ? "Replying..." : "Reply"}
            </button>
          </div>
        </form>
      )}

      {showReplies && hasReplies && (
        <div className="replies">
          {comment.replies.map((reply) => (
            <CommentItem 
              key={reply.commentId} 
              comment={reply} 
              onReply={onReply}
              level={level + 1}
              postId={postId}
              postOwnerId={postOwnerId}
            />
          ))}
        </div>
      )}


      {/* Report Modal */}
      {showReportModal && (
        <div className="forum-comment-modal-overlay" onClick={() => setShowReportModal(false)}>
          <div className="forum-comment-modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="forum-comment-modal-header">
              <h3>Report Comment</h3>
              <button 
                className="forum-comment-modal-close"
                onClick={() => {
                  setShowReportModal(false);
                  setReportReason("");
                }}
              >
                <X size={20} />
              </button>
            </div>
            <form onSubmit={handleSubmitReport}>
              <div className="forum-comment-modal-body">
                <p>Please provide a reason for reporting this comment:</p>
                <textarea
                  value={reportReason}
                  onChange={(e) => setReportReason(e.target.value)}
                  className="forum-comment-report-textarea"
                  placeholder="Enter reason for reporting..."
                  required
                  rows={4}
                />
              </div>
              <div className="forum-comment-modal-footer">
                <button
                  type="button"
                  className="forum-comment-btn forum-comment-btn-secondary"
                  onClick={() => {
                    setShowReportModal(false);
                    setReportReason("");
                  }}
                  disabled={isSubmittingReport}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="forum-comment-btn forum-comment-btn-primary"
                  disabled={isSubmittingReport || !reportReason.trim()}
                >
                  {isSubmittingReport ? "Submitting..." : "Submit Report"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Notification Popup */}
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
