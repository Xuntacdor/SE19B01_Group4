import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { Pin } from "lucide-react";
import { formatTimeVietnam } from "../../utils/date";

export default function PostItem({ post, onPostUpdated, isInClosedSection = false }) {
  const navigate = useNavigate();
  const [isPinned, setIsPinned] = useState(post.isPinned || false);

  // Sync state with props when component mounts or props change
  useEffect(() => {
    setIsPinned(post.isPinned || false);
  }, [post.isPinned]);

  const handlePostClick = () => {
    navigate(`/post/${post.postId}`);
  };

  const formatTime = (dateString) => {
    return formatTimeVietnam(dateString);
  };

  return (
    <div className="post-item" onClick={handlePostClick}>
      {/* Author Column */}
      <div className="post-author">
        <img
          src={post.user?.avatar || "/default-avatar.png"}
          alt={post.user?.username}
          className="post-author-avatar"
        />
      </div>

      {/* Main Content - Topic Column */}
      <div className="post-content">
        <h3 className="post-title">
          {isPinned && (
            <span className="pinned-indicator" title="Bài viết đã được ghim">
              <Pin size={16} />
            </span>
          )}
          {post.title}
        </h3>
        <p className="post-description">
          {post.content && post.content.length > 150
            ? `${post.content.substring(0, 150)}...`
            : post.content || ''}
        </p>
        
        {/* Tags Section */}
        {post.tags && post.tags.length > 0 && (
          <div className="post-tags">
            {post.tags.map((tag, index) => (
              <span key={index} className="post-tag">
                #{tag.tagName}
              </span>
            ))}
          </div>
        )}
      </div>

      {/* Replies Column */}
      <div className="post-stats-replies">
        {post.commentCount || 0}
      </div>

      {/* Views Column */}
      <div className="post-stats-views">
        {post.viewCount || 0}
      </div>

      {/* Activity Column */}
      <div className="post-stats-activity">
        {formatTime(post.createdAt)}
      </div>
    </div>
  );
}