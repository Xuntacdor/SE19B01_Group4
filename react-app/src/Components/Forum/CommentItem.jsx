import React, { useState, useEffect } from "react";
import { likeComment, unlikeComment, createComment, updateComment, deleteComment, reportComment } from "../../Services/ForumApi";
import { ThumbsUp, Edit, Trash2, MoreHorizontal, Flag } from "lucide-react";
import useAuth from "../../Hook/UseAuth";
import { formatTimeVietnam } from "../../utils/date";
import NotificationPopup from "../Forum/NotificationPopup";

export default function CommentItem({ comment, onReply, level = 0, postId, postOwnerId }) {
  const { user } = useAuth();
  const [isVoted, setIsVoted] = useState(comment.isVoted || false);
  const [voteCount, setVoteCount] = useState(comment.voteCount || 0);
  const [showReplyForm, setShowReplyForm] = useState(false);
  const [replyText, setReplyText] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [showReplies, setShowReplies] = useState(false); // Đóng replies mặc định
  const [isEditing, setIsEditing] = useState(false);
  const [editText, setEditText] = useState(comment.content);
  const [showMenu, setShowMenu] = useState(false);
  const [showReportModal, setShowReportModal] = useState(false);
  const [reportReason, setReportReason] = useState("");
  
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
  }, [comment.isVoted, comment.voteCount, comment.likeNumber]);

  // Close menu when clicking outside or when other comment menus are opened
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (showMenu && !event.target.closest('.comment-menu')) {
        setShowMenu(false);
      }
    };

    const handleCloseAllMenus = (event) => {
      if (event.detail.excludeCommentId !== comment.commentId) {
        setShowMenu(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    window.addEventListener('closeAllCommentMenus', handleCloseAllMenus);
    
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      window.removeEventListener('closeAllCommentMenus', handleCloseAllMenus);
    };
  }, [showMenu, comment.commentId]);

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

  const handleEditComment = () => {
    setIsEditing(true);
    setEditText(comment.content);
  };

  const handleSaveEdit = () => {
    if (!editText.trim()) return;
    
    setSubmitting(true);
    updateComment(comment.commentId, editText.trim())
      .then(() => {
        comment.content = editText.trim();
        setIsEditing(false);
      })
      .catch(error => {
        console.error("Error updating comment:", error);
        alert("Error updating comment. Please try again.");
      })
      .finally(() => {
        setSubmitting(false);
      });
  };

  const handleCancelEdit = () => {
    setIsEditing(false);
    setEditText(comment.content);
  };

  const handleDeleteComment = () => {
    if (window.confirm("Are you sure you want to delete this comment?")) {
      setSubmitting(true);
      deleteComment(postId, comment.commentId)
        .then(() => {
          // Gọi callback để xóa comment khỏi danh sách
          if (onReply) {
            onReply({ action: 'delete', commentId: comment.commentId });
          }
        })
        .catch(error => {
          console.error("Error deleting comment:", error);
          alert("Error deleting comment. Please try again.");
        })
        .finally(() => {
          setSubmitting(false);
        });
    }
  };

  const handleReportComment = () => {
    setShowReportModal(true);
    setShowMenu(false);
  };

  const handleSubmitReport = () => {
    if (!reportReason.trim()) return;

    setSubmitting(true);
    reportComment(comment.commentId, reportReason.trim())
      .then(() => {
        showNotification(
          "success",
          "Comment Reported Successfully!",
          "Our moderators will review the reported comment."
        );
        setShowReportModal(false);
        setReportReason("");
      })
      .catch(error => {
        console.error("Error reporting comment:", error);
        showNotification(
          "error",
          "Error Reporting Comment",
          "An error occurred while reporting the comment. Please try again."
        );
      })
      .finally(() => {
        setSubmitting(false);
      });
  };

  const handleCancelReport = () => {
    setShowReportModal(false);
    setReportReason("");
  };

  const handleMenuClick = (e) => {
    e.stopPropagation();
    // Close all other comment menus by dispatching a custom event
    const closeAllMenusEvent = new CustomEvent('closeAllCommentMenus', {
      detail: { excludeCommentId: comment.commentId }
    });
    window.dispatchEvent(closeAllMenusEvent);
    setShowMenu(!showMenu);
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
          <span className="comment-username">@{comment.user?.username}</span>
          <div className="comment-content">
            {isEditing ? (
              <div className="edit-comment-form">
                <textarea
                  value={editText}
                  onChange={(e) => setEditText(e.target.value)}
                  className="edit-textarea"
                  rows={3}
                />
                <div className="edit-actions">
                  <button 
                    className="btn-save" 
                    onClick={handleSaveEdit}
                    disabled={submitting}
                  >
                    Save
                  </button>
                  <button 
                    className="btn-cancel" 
                    onClick={handleCancelEdit}
                    disabled={submitting}
                  >
                    Cancel
                  </button>
                </div>
              </div>
            ) : (
              comment.content
            )}
          </div>
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
          
          {/* 3-dot menu */}
          {user && comment.user && (
            <div className="comment-menu">
              <button
                className="comment-action-btn menu-btn"
                onClick={handleMenuClick}
              >
                <MoreHorizontal size={16} />
              </button>
              
              {showMenu && (
                <div className="comment-menu-dropdown">
                  {/* Edit button - chỉ hiện cho owner của comment */}
                  {user.userId === comment.user.userId && (
                    <button
                      className="menu-item"
                      onClick={() => {
                        handleEditComment();
                        setShowMenu(false);
                      }}
                      disabled={submitting}
                    >
                      <Edit size={16} />
                      Edit
                    </button>
                  )}
                  
                  {/* Delete button - hiện cho owner của comment HOẶC chủ bài viết */}
                  {(user.userId === comment.user.userId || (postOwnerId && user.userId === postOwnerId)) && (
                    <button
                      className="menu-item delete-item"
                      onClick={() => {
                        handleDeleteComment();
                        setShowMenu(false);
                      }}
                      disabled={submitting}
                    >
                      <Trash2 size={16} />
                      Delete
                    </button>
                  )}
                  
                  {/* Report button - hiện cho tất cả users trừ owner của comment */}
                  {user.userId !== comment.user.userId && (
                    <button
                      className="menu-item report-item"
                      onClick={handleReportComment}
                      disabled={submitting}
                    >
                      <Flag size={16} />
                      Report
                    </button>
                  )}
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {showReplyForm && (
        <form className="reply-form" onSubmit={handleReply}>
          <textarea
            value={replyText}
            onChange={(e) => setReplyText(e.target.value)}
            placeholder="Type your reply..."
            rows={3}
            required
          />
          <div className="reply-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setShowReplyForm(false)}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
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

      {/* Report Comment Modal */}
      {showReportModal && (
        <div className="modal-overlay" onClick={handleCancelReport}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Report Comment</h3>
              <button className="modal-close" onClick={handleCancelReport}>×</button>
            </div>
            <div className="modal-body">
              <p>Please provide a reason for reporting this comment:</p>
              <textarea
                value={reportReason}
                onChange={(e) => setReportReason(e.target.value)}
                placeholder="Enter your reason here..."
                rows={4}
                className="report-textarea"
                autoFocus
              />
            </div>
            <div className="modal-actions">
              <button
                className="btn btn-secondary"
                onClick={handleCancelReport}
                disabled={submitting}
              >
                Cancel
              </button>
              <button
                className="btn btn-primary"
                onClick={handleSubmitReport}
                disabled={submitting || !reportReason.trim()}
              >
                {submitting ? "Reporting..." : "Report"}
              </button>
            </div>
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
