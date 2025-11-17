import React, { useState, useEffect, useRef } from "react";
import {
  likeComment,
  unlikeComment,
  createComment,
  reportComment,
  updateComment,
  deleteComment,
} from "../../Services/ForumApi";

import { ThumbsUp, MoreVertical, Flag, Edit, Trash2, X } from "lucide-react";

import useAuth from "../../Hook/UseAuth";
import { formatTimeVietnam } from "../../utils/date";
import NotificationPopup from "../Forum/NotificationPopup";
import DeleteConfirmPopup from "../Common/DeleteConfirmPopup";

export default function CommentItem({
  comment,
  onReply,
  level = 0,
  postId,
  postOwnerId,
}) {
  const { user } = useAuth();

  const [isVoted, setIsVoted] = useState(comment.isVoted || false);
  const [voteCount, setVoteCount] = useState(
    comment.voteCount || comment.likeNumber || 0
  );

  const [showReplyForm, setShowReplyForm] = useState(false);
  const [replyText, setReplyText] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const [showReplies, setShowReplies] = useState(false);
  const [showMenu, setShowMenu] = useState(false);

  const [showReportModal, setShowReportModal] = useState(false);
  const [reportReason, setReportReason] = useState("");
  const [isSubmittingReport, setIsSubmittingReport] = useState(false);

  const [showEditForm, setShowEditForm] = useState(false);
  const [editContent, setEditContent] = useState(comment.content);
  const [isSubmittingEdit, setIsSubmittingEdit] = useState(false);

  const [showDeletePopup, setShowDeletePopup] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const menuRef = useRef(null);

  // Notification state
  const [notification, setNotification] = useState({
    isOpen: false,
    type: "success",
    title: "",
    message: "",
  });

  /** Sync when comment changes */
  useEffect(() => {
    setIsVoted(comment.isVoted || false);
    setVoteCount(comment.voteCount || comment.likeNumber || 0);
    setEditContent(comment.content);
  }, [comment]);

  /** Close menu when clicking outside */
  useEffect(() => {
    const handler = (e) => {
      if (menuRef.current && !menuRef.current.contains(e.target)) {
        setShowMenu(false);
      }
    };

    if (showMenu) document.addEventListener("mousedown", handler);

    return () => {
      document.removeEventListener("mousedown", handler);
    };
  }, [showMenu]);

  /** Notification helpers */
  const showNotification = (type, title, message) => {
    setNotification({
      isOpen: true,
      type,
      title,
      message,
    });
  };

  const closeNotification = () =>
    setNotification((prev) => ({ ...prev, isOpen: false }));

  /** Vote */
  const handleVote = () => {
    if (isVoted) {
      unlikeComment(comment.commentId)
        .then(() => {
          setIsVoted(false);
          setVoteCount((v) => v - 1);
        })
        .catch(console.error);
    } else {
      likeComment(comment.commentId)
        .then(() => {
          setIsVoted(true);
          setVoteCount((v) => v + 1);
        })
        .catch(console.error);
    }
  };

  /** Reply */
  const handleReply = (e) => {
    e.preventDefault();
    if (!replyText.trim()) return;

    setSubmitting(true);
    createComment(postId, replyText, comment.commentId)
      .then((res) => {
        const replyData = {
          ...res.data,
          parentCommentId: comment.commentId,
        };

        onReply(replyData);
        setReplyText("");
        setShowReplyForm(false);

        showNotification(
          "success",
          "Reply Posted!",
          "Your reply has been added."
        );
      })
      .catch((error) => {
        const msg =
          error.response?.data?.message ||
          error.response?.data ||
          error.message ||
          "";

        if (msg.toLowerCase().includes("restricted")) {
          showNotification(
            "error",
            "Account Restricted",
            "Your account is restricted from commenting."
          );
        } else {
          showNotification("error", "Reply Failed", "Please try again later.");
        }
      })
      .finally(() => setSubmitting(false));
  };

  /** Delete */
  const handleDelete = async () => {
    setIsDeleting(true);

    try {
      await deleteComment(postId, comment.commentId);

      onReply({ action: "delete", commentId: comment.commentId });
      showNotification(
        "success",
        "Comment Deleted",
        "The comment has been removed."
      );
    } catch (error) {
      const msg =
        error.response?.data?.message ||
        error.response?.data ||
        error.message ||
        "Failed to delete";

      showNotification("error", "Delete Failed", msg);
    } finally {
      setIsDeleting(false);
      setShowDeletePopup(false);
    }
  };

  /** Report comment */
  const handleSubmitReport = (e) => {
    e.preventDefault();
    if (!reportReason.trim()) return;

    setIsSubmittingReport(true);

    reportComment(comment.commentId, reportReason.trim())
      .then(() => {
        showNotification(
          "success",
          "Reported",
          "Your report has been submitted."
        );
        setReportReason("");
        setShowReportModal(false);
      })
      .catch((error) => {
        const msg =
          error.response?.data?.message ||
          error.response?.data ||
          error.message ||
          "Report failed";

        showNotification("error", "Report Failed", msg);
      })
      .finally(() => setIsSubmittingReport(false));
  };

  /** Update comment */
  const handleSubmitEdit = (e) => {
    e.preventDefault();
    if (!editContent.trim() || editContent === comment.content) return;

    setIsSubmittingEdit(true);

    updateComment(comment.commentId, editContent.trim())
      .then(() => {
        comment.content = editContent.trim();
        setShowEditForm(false);

        showNotification(
          "success",
          "Comment Updated",
          "Your comment has been updated."
        );
      })
      .catch((error) => {
        const msg =
          error.response?.data?.message ||
          error.response?.data ||
          error.message ||
          "Update failed";

        showNotification("error", "Update Failed", msg);
      })
      .finally(() => setIsSubmittingEdit(false));
  };

  /** Permissions */
  const isOwner = user && comment.user?.userId === user.userId;
  const isAdmin = user && user.role === "admin";
  const isPostOwner = user && postOwnerId === user.userId;

  const canEdit = isOwner;
  const canDelete = isOwner || isAdmin || isPostOwner;
  const canReport = user && !isOwner;

  /** Count nested replies */
  const countReplies = (items) => {
    if (!items) return 0;
    let total = items.length;
    items.forEach((r) => (total += countReplies(r.replies)));
    return total;
  };

  const replyCount = countReplies(comment.replies || []);

  return (
    <div className={`comment-item ${level > 0 ? "reply-item" : ""}`}>
      {/* HEADER */}
      <div className="comment-header">
        <img
          src={comment.user?.avatar || "/default-avatar.png"}
          alt="avatar"
          className="comment-avatar"
        />

        <div className="comment-user-info">
          <div className="comment-user-header">
            <span className="comment-username">@{comment.user?.username}</span>

            {(canEdit || canDelete || canReport) && (
              <div className="comment-menu" ref={menuRef}>
                <button
                  className="comment-menu-btn"
                  onClick={() => setShowMenu((v) => !v)}
                >
                  <MoreVertical size={16} />
                </button>

                {showMenu && (
                  <div className="comment-menu-dropdown">
                    {canReport && (
                      <button
                        className="forum-comment-menu-item report-item"
                        onClick={() => {
                          setShowMenu(false);
                          setShowReportModal(true);
                        }}
                      >
                        <Flag size={16} />
                        Report
                      </button>
                    )}

                    {canEdit && (
                      <button
                        className="forum-comment-menu-item"
                        onClick={() => {
                          setShowMenu(false);
                          setShowEditForm(true);
                        }}
                      >
                        <Edit size={16} />
                        Edit
                      </button>
                    )}

                    {canDelete && (
                      <button
                        className="forum-comment-menu-item delete-item"
                        onClick={() => {
                          setShowMenu(false);
                          setShowDeletePopup(true);
                        }}
                      >
                        <Trash2 size={16} />
                        Delete
                      </button>
                    )}
                  </div>
                )}
              </div>
            )}
          </div>

          {/* CONTENT OR EDIT FORM */}
          {showEditForm ? (
            <form className="edit-comment-form" onSubmit={handleSubmitEdit}>
              <textarea
                value={editContent}
                onChange={(e) => setEditContent(e.target.value)}
                className="forum-comment-edit-textarea"
                rows={3}
              />

              <div className="edit-actions">
                <button
                  type="button"
                  className="forum-comment-btn forum-comment-btn-secondary"
                  onClick={() => setShowEditForm(false)}
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  className="forum-comment-btn forum-comment-btn-primary"
                  disabled={isSubmittingEdit}
                >
                  {isSubmittingEdit ? "Saving..." : "Save"}
                </button>
              </div>
            </form>
          ) : (
            <div className="comment-content">{comment.content}</div>
          )}
        </div>
      </div>

      {/* ACTIONS */}
      <div className="comment-actions">
        <div className="comment-actions-left">
          <span className="comment-time">
            {formatTimeVietnam(comment.createdAt)}
          </span>

          <button
            className="comment-action-btn"
            onClick={() => setShowReplyForm((v) => !v)}
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
          {replyCount > 0 && (
            <button
              className="comment-action-btn toggle-replies-btn"
              onClick={() => setShowReplies((v) => !v)}
            >
              {showReplies
                ? `Hide ${replyCount} replies`
                : `View all ${replyCount} replies`}
            </button>
          )}
        </div>
      </div>

      {/* REPLY FORM */}
      {showReplyForm && (
        <form className="reply-form" onSubmit={handleReply}>
          <textarea
            value={replyText}
            onChange={(e) => setReplyText(e.target.value)}
            rows={4}
            placeholder="Type your reply..."
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
              disabled={submitting}
            >
              {submitting ? "Replying..." : "Reply"}
            </button>
          </div>
        </form>
      )}

      {/* REPLIES */}
      {showReplies && comment.replies?.length > 0 && (
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

      {/* DELETE POPUP */}
      <DeleteConfirmPopup
        show={showDeletePopup}
        title="Delete Comment"
        message="Are you sure you want to delete this comment? This action cannot be undone."
        onCancel={() => setShowDeletePopup(false)}
        onConfirm={handleDelete}
      />

      {/* REPORT MODAL */}
      {showReportModal && (
        <div
          className="forum-comment-modal-overlay"
          onClick={() => setShowReportModal(false)}
        >
          <div
            className="forum-comment-modal-content"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="forum-comment-modal-header">
              <h3>Report Comment</h3>
              <button
                className="forum-comment-modal-close"
                onClick={() => setShowReportModal(false)}
              >
                <X size={20} />
              </button>
            </div>

            <form onSubmit={handleSubmitReport}>
              <div className="forum-comment-modal-body">
                <p>Please provide a reason for reporting:</p>
                <textarea
                  value={reportReason}
                  onChange={(e) => setReportReason(e.target.value)}
                  className="forum-comment-report-textarea"
                  rows={4}
                />
              </div>

              <div className="forum-comment-modal-footer">
                <button
                  type="button"
                  className="forum-comment-btn forum-comment-btn-secondary"
                  onClick={() => setShowReportModal(false)}
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  className="forum-comment-btn forum-comment-btn-primary"
                  disabled={isSubmittingReport}
                >
                  {isSubmittingReport ? "Submitting..." : "Submit Report"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* NOTIFICATION */}
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
