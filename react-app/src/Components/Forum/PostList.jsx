import React, { useEffect, useRef, useCallback } from "react";
import PostItem from "./PostItem";
import NothingFound from "../Nothing/NothingFound";
import sadCloud from "../../assets/sad_cloud.png";
export default function PostList({ 
  posts, 
  loading, 
  initialLoading,
  onLoadMore, 
  hasMore, 
  onPostUpdated, 
  isInClosedSection = false 
}) {
  const loadingRef = useRef(null);
  const observerRef = useRef(null);

  // Memoize callback to prevent observer recreation
  const handleIntersection = useCallback((entries) => {
    const [entry] = entries;
    if (entry.isIntersecting && hasMore && !loading) {
      onLoadMore();
    }
  }, [hasMore, loading, onLoadMore]);

  // Setup Intersection Observer
  useEffect(() => {
    // Cleanup previous observer
    if (observerRef.current) {
      observerRef.current.disconnect();
    }

    // Create new observer
    observerRef.current = new IntersectionObserver(handleIntersection, {
      threshold: 0.1,
      rootMargin: '100px' // Start loading before reaching the bottom
    });

    // Observe loading element
    if (loadingRef.current) {
      observerRef.current.observe(loadingRef.current);
    }

    // Cleanup on unmount
    return () => {
      if (observerRef.current) {
        observerRef.current.disconnect();
      }
    };
  }, [handleIntersection]);

  if (initialLoading) {
    return (
      <div className="posts-container">
        <div className="posts-header">
          <div className="header-author">Author</div>
          <div className="header-topic">Topic</div>
          <div className="header-replies">Replies</div>
          <div className="header-views">Views</div>
          <div className="header-activity">Activity</div>
        </div>
        <div className="initial-loading">
          <div className="loading-dots">
            <span></span>
            <span></span>
            <span></span>
          </div>
        </div>
      </div>
    );
  }

  if (posts.length === 0 && !loading) {
    return (
      <NothingFound
        imageSrc= {sadCloud}
        title="No posts found"
        message="Be the first to start a discussion."
        actionLabel="Create a post"
        to="/create-post"
      />
    );
  }

  return (
    <div className="posts-container">
      {/* Table-like header */}
      <div className="posts-header">
        <div className="header-author">Author</div>
        <div className="header-topic">Topic</div>
        <div className="header-replies">Replies</div>
        <div className="header-views">Views</div>
        <div className="header-activity">Activity</div>
      </div>
      
      {/* Posts with fade-in animation */}
      <div className="posts-list">
        {posts.map((post, index) => (
          <div 
            key={post.postId || post.id || index} 
            className="post-item-wrapper"
            style={{ animationDelay: `${Math.min(index * 0.05, 0.5)}s` }}
          >
            <PostItem 
              post={post} 
              onPostUpdated={onPostUpdated} 
              isInClosedSection={isInClosedSection} 
            />
          </div>
        ))}
      </div>
      
      {/* Loading indicator for infinite scroll */}
      {hasMore && (
        <div ref={loadingRef} className="infinite-loading">
          {loading && (
            <div className="infinite-loading-indicator">
              <div className="loading-dots-small">
                <span></span>
                <span></span>
                <span></span>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}