import React, { useState, useEffect } from "react";
import { likeComment, unlikeComment, createComment } from "../../Services/ForumApi";
import { ThumbsUp } from "lucide-react";
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
            {comment.content}
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
