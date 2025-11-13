import React, { useState, useEffect, useCallback, useRef } from "react";
import "./Forum.css";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import PostList from "../../Components/Forum/PostList";
import RightSidebar from "../../Components/Forum/RightSidebar";
import { getPostsByFilter, getTags } from "../../Services/ForumApi";
import { useNavigate } from "react-router-dom";
import { Plus } from "lucide-react";
import Cloud from "../../assets/sad_cloud.png"

export default function Forum() {
  const [posts, setPosts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [initialLoading, setInitialLoading] = useState(true);
  const [activeFilter, setActiveFilter] = useState("new");
  const [currentPage, setCurrentPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [selectedTag, setSelectedTag] = useState("all");
  const [tags, setTags] = useState([]);
  const abortControllerRef = useRef(null);

  const navigate = useNavigate();

  const filters = [
    { key: "new", label: "New" },
    { key: "hot", label: "Hot" },
    { key: "closed", label: "Closed" },
  ];

  // Load tags on mount
  useEffect(() => {
    loadTags();
  }, []);

  // Debounced filter change handler
  useEffect(() => {
    const timer = setTimeout(() => {
      loadPosts(true);
    }, 150); // Debounce 150ms

    return () => clearTimeout(timer);
  }, [activeFilter, selectedTag]);

  const loadTags = () => {
    getTags()
      .then((response) => {
        setTags(response.data || []);
      })
      .catch((error) => {
        console.error("Error loading tags:", error);
      });
  };

  const loadPosts = useCallback((reset = false, pageOverride = null) => {
    // Cancel previous request if exists
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    // Create new abort controller
    abortControllerRef.current = new AbortController();

    setLoading(true);
    
    if (reset) {
      setPosts([]);
      setHasMore(true);
      setInitialLoading(true);
    }

    // Use pageOverride if provided, otherwise use currentPage from state
    const pageToLoad = pageOverride !== null ? pageOverride : (reset ? 1 : currentPage);
    
    getPostsByFilter(activeFilter, pageToLoad, 10, selectedTag !== 'all' ? selectedTag : null)
      .then((response) => {
        if (abortControllerRef.current?.signal.aborted) return;
        
        const newPosts = response.data || [];
        
        if (reset) {
          setPosts(newPosts);
          setCurrentPage(2);
        } else {
          setPosts((prev) => [...prev, ...newPosts]);
          setCurrentPage((prev) => prev + 1);
        }
        
        setHasMore(newPosts.length === 10);
      })
      .catch((error) => {
        if (error.name === 'AbortError') return;
        console.error("Error loading posts:", error);
      })
      .finally(() => {
        if (abortControllerRef.current?.signal.aborted) return;
        setLoading(false);
        setInitialLoading(false);
      });
  }, [activeFilter, selectedTag, currentPage]);

  const loadMorePosts = useCallback(() => {
    if (!hasMore || loading) return;
    loadPosts(false, currentPage);
  }, [hasMore, loading, loadPosts, currentPage]);

  const handleFilterChange = (filter) => {
    if (filter === activeFilter) return;
    setActiveFilter(filter);
  };

  const handleTagChange = (e) => {
    const newTag = e.target.value;
    if (newTag === selectedTag) return;
    setSelectedTag(newTag);
  };

  const handlePostUpdated = useCallback(() => {
    loadPosts(true);
  }, [loadPosts]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, []);

  return (
    <AppLayout title="Forum" sidebar={<GeneralSidebar />}>
      <div className="forum-content">
        <div className="forum-main">
          <div className="forum-header">
            <select 
              className="tag-filter-select"
              value={selectedTag}
              onChange={handleTagChange}
              disabled={loading && initialLoading}
            >
              <option value="all">All Posts</option>
              {tags.map((tag) => (
                <option key={tag.tagId} value={tag.tagName}>
                  #{tag.tagName}
                </option>
              ))}
            </select>
            <button
              className="ask-question-btn"
              onClick={() => navigate("/create-post")}
            >
              <Plus size={16} />
              Create a post
            </button>
          </div>

          <div className="forum-filters">
            {filters.map((filter) => (
              <button
                key={filter.key}
                className={`filter-btn ${
                  activeFilter === filter.key ? "active" : ""
                }`}
                onClick={() => handleFilterChange(filter.key)}
              >
                {filter.label}
              </button>
            ))}
          </div>

          <PostList
            posts={posts}
            loading={loading}
            initialLoading={initialLoading}
            onLoadMore={loadMorePosts}
            hasMore={hasMore}
            onPostUpdated={handlePostUpdated}
            isInClosedSection={activeFilter === "closed"}
          />
        </div>

        <RightSidebar />
      </div>
    </AppLayout>
  );
}