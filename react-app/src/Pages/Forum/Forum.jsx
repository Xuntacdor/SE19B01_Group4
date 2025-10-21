import React, { useState, useEffect } from "react";
import "./Forum.css";
import AppLayout from "../../Components/Layout/AppLayout";
import GeneralSidebar from "../../Components/Layout/GeneralSidebar";
import PostList from "../../Components/Forum/PostList";
import RightSidebar from "../../Components/Forum/RightSidebar";
import CreatePost from "../../Components/Forum/CreatePost";
import { getPostsByFilter, getTags } from "../../Services/ForumApi";
import { useNavigate } from "react-router-dom";
import { Plus } from "lucide-react";

export default function Forum() {
  const [posts, setPosts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeFilter, setActiveFilter] = useState("new");
  const [currentPage, setCurrentPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [showCreatePost, setShowCreatePost] = useState(false);
  const [selectedTag, setSelectedTag] = useState("all");
  const [tags, setTags] = useState([]);

  const navigate = useNavigate();

  const filters = [
    { key: "new", label: "New" },
    { key: "hot", label: "Hot" },
    { key: "closed", label: "Closed" },
  ];

  useEffect(() => {
    loadPosts();
  }, [activeFilter, selectedTag]);

  useEffect(() => {
    loadTags();
  }, []);

  const loadTags = () => {
    getTags()
      .then((response) => {
        setTags(response.data || []);
      })
      .catch((error) => {
        console.error("Error loading tags:", error);
      });
  };

  const loadPosts = () => {
    setLoading(true);
    getPostsByFilter(activeFilter, 1)
      .then((response) => {
        let filteredPosts = response.data;
        
        // Filter by selected tag if not "all"
        if (selectedTag !== "all") {
          filteredPosts = response.data.filter(post => 
            post.tags && post.tags.some(tag => tag.tagName === selectedTag)
          );
        }
        
        setPosts(filteredPosts);
        setCurrentPage(1);
        setHasMore(filteredPosts.length === 10);
      })
      .catch((error) => {
        console.error("Error loading posts:", error);
      })
      .finally(() => setLoading(false));
  };

  const loadMorePosts = () => {
    if (!hasMore || loading) return;

    setLoading(true);
    getPostsByFilter(activeFilter, currentPage + 1)
      .then((response) => {
        let filteredPosts = response.data;
        
        // Filter by selected tag if not "all"
        if (selectedTag !== "all") {
          filteredPosts = response.data.filter(post => 
            post.tags && post.tags.some(tag => tag.tagName === selectedTag)
          );
        }
        
        setPosts((prev) => [...prev, ...filteredPosts]);
        setCurrentPage((prev) => prev + 1);
        setHasMore(filteredPosts.length === 10);
      })
      .catch((error) => console.error("Error loading more posts:", error))
      .finally(() => setLoading(false));
  };

  const handleFilterChange = (filter) => {
    setActiveFilter(filter);
  };

  const handleTagChange = (e) => {
    setSelectedTag(e.target.value);
  };

  const handlePostUpdated = () => loadPosts();

  const handlePostCreated = (newPost) => {
    setPosts(prevPosts => [newPost, ...prevPosts]);
    setShowCreatePost(false);
  };

  return (
    <AppLayout title="Forum" sidebar={<GeneralSidebar />}>
      <div className="forum-content">
        <div className="forum-main">
          <div className="forum-header">
            <select 
              className="tag-filter-select"
              value={selectedTag}
              onChange={handleTagChange}
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
              onClick={() => setShowCreatePost(true)}
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
            onLoadMore={loadMorePosts}
            hasMore={hasMore}
            onPostUpdated={handlePostUpdated}
            isInClosedSection={activeFilter === "closed"}
          />
        </div>

        <RightSidebar />
      </div>

      <CreatePost
        isOpen={showCreatePost}
        onClose={() => setShowCreatePost(false)}
        onPostCreated={handlePostCreated}
      />
    </AppLayout>
  );
}
