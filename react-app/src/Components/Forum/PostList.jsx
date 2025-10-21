import React, { useEffect, useRef } from "react";
import PostItem from "./PostItem";
import NothingFound from "../Nothing/NothingFound";

export default function PostList({ posts, loading, onLoadMore, hasMore, onPostUpdated, isInClosedSection = false }) {
  const observerRef = useRef();
  const loadingRef = useRef();

  // Infinite scroll observer
  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loading) {
          onLoadMore();
        }
      },
      { threshold: 0.1 }
    );

    if (loadingRef.current) {
      observer.observe(loadingRef.current);
    }

    return () => {
      if (loadingRef.current) {
        observer.unobserve(loadingRef.current);
      }
    };
  }, [hasMore, loading, onLoadMore]);

  if (loading && posts.length === 0) {
    return (
      <div className="loading">
        <div>Loading posts...</div>
      </div>
    );
  }

  if (posts.length === 0) {
    return (
      <NothingFound
        imageSrc="/src/assets/sad_cloud.png"
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
      
      {posts.map((post) => (
        <PostItem key={post.postId} post={post} onPostUpdated={onPostUpdated} isInClosedSection={isInClosedSection} />
      ))}
      
      {/* Loading indicator for infinite scroll */}
      {hasMore && (
        <div ref={loadingRef} className="infinite-loading">
          {loading ? (
            <div className="loading-spinner">
              <div className="spinner"></div>
              <span>Loading more posts...</span>
            </div>
          ) : (
            <div className="scroll-hint">
              <span>Scroll down to load more</span>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
