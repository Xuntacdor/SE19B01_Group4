import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { getAllTags, createTag, updateTag, deleteTag } from "../../Services/TagApi";
import { getMe, logout } from "../../Services/AuthApi";
import { formatTimeVietnam } from "../../utils/date";
import {
  FileText,
  AlertTriangle,
  XCircle,
  CheckCircle,
  Users,
  MessageSquare,
  Check,
  X,
  Bell,
  User,
  Calendar,
  TrendingUp,
  Eye,
  ThumbsUp,
  Flag,
  Clock,
  Tag,
  BarChart3,
  Filter,
  Search,
  Plus,
  Edit,
  Trash2,
  LogOut,
  Settings,
  LayoutDashboard,
  LayoutList,
  Sparkles,
  Loader
} from "lucide-react";
import "./ModeratorDashboard.css";
import { Line } from "react-chartjs-2";
import { Chart as ChartJS, LineElement, CategoryScale, LinearScale, PointElement, Tooltip, Legend } from "chart.js";
import * as ModeratorApi from "../../Services/ModeratorApi";
import { approveReport, dismissReport, analyzePost } from "../../Services/ModeratorApi";
import { getPostsByFilter } from "../../Services/ForumApi";
import NotificationPopup from "../../Components/Forum/NotificationPopup";
import RejectionReasonPopup from "../../Components/Common/RejectionReasonPopup";
import CommentSection from "../../Components/Forum/CommentSection";
import { marked } from "marked";
import AppLayout from "../../Components/Layout/AppLayout";
import ModeratorNavbar from "../../Components/Moderator/ModeratorNavbar";

ChartJS.register(LineElement, CategoryScale, LinearScale, PointElement, Tooltip, Legend);

export default function ModeratorDashboard() {
  const navigate = useNavigate();
  const [currentView, setCurrentView] = useState("overview");

  // Menu items for Moderator Sidebar
  const menuItems = [
    {
      icon: <LayoutDashboard size={20} />,
      label: "All Posts",
      view: "overview"
    },
    {
      icon: <BarChart3 size={20} />,
      label: "Statistics",
      view: "statistics"
    },
    {
      icon: <Tag size={20} />,
      label: "Tag Management",
      view: "tags"
    },
    {
      icon: <FileText size={20} />,
      label: "Pending Posts",
      view: "pending"
    },
    {
      icon: <Flag size={20} />,
      label: "Reported Comments",
      view: "reported"
    },
    {
      icon: <XCircle size={20} />,
      label: "Rejected Posts",
      view: "rejected"
    }
  ];
  const [selectedMonth, setSelectedMonth] = useState(new Date().getMonth() + 1);
  const [stats, setStats] = useState({
    total: 0,
    pending: 0,
    reported: 0,
    rejected: 0
  });
  const [chartData, setChartData] = useState([]);
  const [users, setUsers] = useState([]);
  const [pendingPosts, setPendingPosts] = useState([]);
  const [reportedComments, setReportedComments] = useState([]);
  const [rejectedPosts, setRejectedPosts] = useState([]);
  const [selectedPost, setSelectedPost] = useState(null);
  const [showPostDetail, setShowPostDetail] = useState(false);
  const [selectedComment, setSelectedComment] = useState(null);
  const [showCommentDetail, setShowCommentDetail] = useState(false);
  const [rejectReason, setRejectReason] = useState("");
  const [showRejectionPopup, setShowRejectionPopup] = useState(false);
  const [postToReject, setPostToReject] = useState(null);
  
  // New state for forum posts
  const [allPosts, setAllPosts] = useState([]);
  const [originalPosts, setOriginalPosts] = useState([]); // Store all posts for filtering
  const [postsLoading, setPostsLoading] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedTag, setSelectedTag] = useState("all");
  
  // Tag management states
  const [tags, setTags] = useState([]);
  const [tagsLoading, setTagsLoading] = useState(false);
  const [showTagModal, setShowTagModal] = useState(false);
  const [editingTag, setEditingTag] = useState(null);
  const [tagName, setTagName] = useState('');
  const [tagError, setTagError] = useState('');
  
  // User states
  const [user, setUser] = useState(null);
  
  // Notification state
  const [notification, setNotification] = useState({
    isOpen: false,
    type: "success",
    title: "",
    message: ""
  });

  // AI Analysis states
  const [postAnalysis, setPostAnalysis] = useState({}); // { postId: analysisResult }
  const [analyzingPost, setAnalyzingPost] = useState(null); // postId being analyzed

  // Load data from API
  useEffect(() => {
    loadDashboardData();
    loadUserData();
  }, [selectedMonth]);

  const loadUserData = async () => {
    try {
      const response = await getMe();
      setUser(response.data);
    } catch (error) {
      console.error('Error loading user data:', error);
    }
  };

  const handleLogout = async () => {
    try {
      await logout();
      navigate('/login');
    } catch (error) {
      console.error('Error logging out:', error);
      navigate('/login');
    }
  };

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

  // Load forum posts when overview is selected
  useEffect(() => {
    if (currentView === "overview") {
      loadAllPosts();
      loadTagsForFilter();
    }
  }, [currentView]);

  // Load tags when tags view is selected
  useEffect(() => {
    if (currentView === 'tags') {
      loadTags();
    }
  }, [currentView]);

  // Filter posts when search query or selected tag changes
  useEffect(() => {
    if (currentView === "overview") {
      filterPosts();
    }
  }, [searchQuery, selectedTag]);

  const loadDashboardData = () => {
    // Load dashboard stats
    ModeratorApi.getModeratorStats()
      .then((statsResponse) => {
        setStats({
          total: statsResponse.data.totalPosts,
          pending: statsResponse.data.pendingPosts,
          reported: statsResponse.data.reportedComments,
          rejected: statsResponse.data.rejectedPosts
        });
      })
      .catch((error) => {
        console.error("Error loading stats:", error);
        setStats({
          total: 150,
          pending: 8,
          reported: 12,
          rejected: 5
        });
      });

    // Load chart data
    const currentYear = new Date().getFullYear();
    ModeratorApi.getPostsChartData(selectedMonth, currentYear)
      .then((chartResponse) => {
        setChartData(chartResponse.data);
      })
      .catch((error) => {
        console.error("Error loading chart data:", error);
        setChartData([
          { label: "1/12", value: 5 },
          { label: "2/12", value: 8 },
          { label: "3/12", value: 12 },
          { label: "4/12", value: 6 },
          { label: "5/12", value: 15 },
          { label: "6/12", value: 9 },
          { label: "7/12", value: 11 }
        ]);
      });

    // Load users data
    ModeratorApi.getUsers()
      .then((usersResponse) => {
        setUsers(usersResponse.data);
      })
      .catch((error) => {
        console.error("Error loading users:", error);
      });

    // Load pending posts
    ModeratorApi.getPendingPosts()
      .then((pendingResponse) => {
        setPendingPosts(pendingResponse.data);
      })
      .catch((error) => {
        console.error("Error loading pending posts:", error);
      });

    // Load reported comments
    ModeratorApi.getReportedComments()
      .then((reportedResponse) => {
        setReportedComments(reportedResponse.data);
      })
      .catch((error) => {
        console.error("Error loading reported comments:", error);
      });

    // Load rejected posts
    ModeratorApi.getRejectedPosts()
      .then((rejectedResponse) => {
        setRejectedPosts(rejectedResponse.data);
      })
      .catch((error) => {
        console.error("Error loading rejected posts:", error);
      });
  };

  const loadAllPosts = async () => {
    try {
      setPostsLoading(true);
      // Load all posts (filter = "all")
      const response = await getPostsByFilter("all", 1, 50);
      const posts = response.data || [];
      setOriginalPosts(posts);
      // Apply current filters after loading
      applyFilters(posts);
    } catch (error) {
      console.error("Error loading posts:", error);
      setOriginalPosts([]);
      setAllPosts([]);
    } finally {
      setPostsLoading(false);
    }
  };

  const loadTagsForFilter = async () => {
    try {
      const response = await getAllTags();
      setTags(response || []);
    } catch (error) {
      console.error('Error loading tags for filter:', error);
      setTags([]);
    }
  };

  const applyFilters = (postsToFilter) => {
    const sourcePosts = postsToFilter || originalPosts;
    
    if (!sourcePosts || sourcePosts.length === 0) {
      setAllPosts([]);
      return;
    }

    let filtered = [...sourcePosts];

    // Filter by search query (title)
    if (searchQuery.trim()) {
      const query = searchQuery.trim().toLowerCase();
      filtered = filtered.filter(post => 
        post.title && post.title.toLowerCase().includes(query)
      );
    }

    // Filter by selected tag
    if (selectedTag && selectedTag !== "all") {
      const tagId = parseInt(selectedTag);
      filtered = filtered.filter(post => 
        post.tags && post.tags.length > 0 && post.tags.some(tag => 
          tag.tagId === tagId || tag.tagName === selectedTag
        )
      );
    }

    setAllPosts(filtered);
  };

  const filterPosts = () => {
    if (originalPosts.length > 0) {
      applyFilters();
    }
  };

  const handleAcceptPost = async (postId) => {
    try {
      await ModeratorApi.approvePost(postId);
      // Update local state
      setPendingPosts(prev => prev.filter(post => (post.postId || post.id) !== postId));
      setStats(prev => ({ ...prev, pending: prev.pending - 1, total: prev.total + 1 }));
      setShowPostDetail(false);
      showNotification(
        "success",
        "Post approved successfully!",
        "Post has been approved and is now visible on the forum."
      );
    } catch (error) {
      console.error("Error approving post:", error);
      showNotification(
        "error",
        "Error approving post",
        "An error occurred while approving the post. Please try again."
      );
    }
  };

  const handleOpenRejectPopup = (postId) => {
    setPostToReject(postId);
    setShowRejectionPopup(true);
  };

  const handleRejectPost = async (reason) => {
    if (!postToReject) return;
    
    try {
      await ModeratorApi.rejectPost(postToReject, reason);
      // Update local state
      setPendingPosts(prev => prev.filter(post => (post.postId || post.id) !== postToReject));
      setStats(prev => ({ ...prev, pending: prev.pending - 1, rejected: prev.rejected + 1 }));
      
      // Reload rejected posts to show the newly rejected post immediately
      try {
        const rejectedResponse = await ModeratorApi.getRejectedPosts();
        setRejectedPosts(rejectedResponse.data);
      } catch (error) {
        console.error("Error loading rejected posts:", error);
      }
      
      setShowPostDetail(false);
      setShowRejectionPopup(false);
      setPostToReject(null);
      showNotification(
        "success",
        "Post rejected successfully!",
        "Post has been rejected and will not be visible on the forum."
      );  
    } catch (error) {
      console.error("Error rejecting post:", error);
      setShowRejectionPopup(false);
      setPostToReject(null);
      showNotification(
        "error",
        "Error rejecting post",
        "An error occurred while rejecting the post. Please try again."
      );
    }
  };

  const handleViewPost = (post, fromPendingList = false) => {
    // Set status if viewing from pending posts list
    const postWithStatus = fromPendingList ? { ...post, status: "pending" } : post;
    setSelectedPost(postWithStatus);
    setShowPostDetail(true);
  };

  const handleViewComment = (comment) => {
    // Hiển thị modal với thông tin comment
    setSelectedComment(comment);
    setShowCommentDetail(true);
  };

  const handleApproveReport = async (reportId) => {
    try {
      // Find the comment ID of the report being approved
      const reportToApprove = reportedComments.find(comment => comment.reportId === reportId);
      const commentIdToDelete = reportToApprove?.commentId;
      
      await approveReport(reportId);
      
      // Remove ALL reports for the same comment (since the comment and all its reports are deleted)
      const reportsToRemove = reportedComments.filter(comment => comment.commentId === commentIdToDelete);
      const removedCount = reportsToRemove.length;
      
      setReportedComments(prev => prev.filter(comment => comment.commentId !== commentIdToDelete));
      setStats(prev => ({ ...prev, reported: prev.reported - removedCount }));
      
      // Reload users data to update reported comment counts
      try {
        const usersResponse = await ModeratorApi.getUsers();
        setUsers(usersResponse.data);
      } catch (userError) {
        console.error("Error reloading users data:", userError);
      }
      
      showNotification(
        "success",
        "Report approved successfully!",
        `The reported comment has been removed from the forum and all ${removedCount} report(s) for this comment have been resolved.`
      );
    } catch (error) {
      console.error("Error approving report:", error);
      showNotification(
        "error",
        "Error approving report",
        "An error occurred while approving the report. Please try again."
      );
    }
  };

  const handleDismissReport = async (reportId) => {
    try {
      await dismissReport(reportId);
      // Update local state by removing the dismissed report
      setReportedComments(prev => prev.filter(comment => comment.reportId !== reportId));
      setStats(prev => ({ ...prev, reported: prev.reported - 1 }));
      showNotification(
        "success",
        "Report dismissed successfully!",
        "The report has been dismissed and the comment remains visible."
      );
    } catch (error) {
      console.error("Error dismissing report:", error);
      showNotification(
        "error",
        "Error dismissing report",
        "An error occurred while dismissing the report. Please try again."
      );
    }
  };

  const handleRestrictUser = async (userId) => {
    try {
      await ModeratorApi.restrictUser(userId);
      
      // Update local state
      setUsers(prevUsers => 
        prevUsers.map(u => 
          u.userId === userId ? { ...u, isRestricted: true } : u
        )
      );
      
      showNotification(
        "success",
        "User restricted successfully!",
        "The user has been restricted from posting and commenting on the forum."
      );
    } catch (error) {
      console.error("Error restricting user:", error);
      showNotification(
        "error",
        "Error restricting user",
        "An error occurred while restricting the user. Please try again."
      );
    }
  };

  const handleUnrestrictUser = async (userId) => {
    try {
      await ModeratorApi.unrestrictUser(userId);
      
      // Update local state
      setUsers(prevUsers => 
        prevUsers.map(u => 
          u.userId === userId ? { ...u, isRestricted: false } : u
        )
      );
      
      showNotification(
        "success",
        "User unrestricted successfully!",
        "The user can now post and comment on the forum again."
      );
    } catch (error) {
      console.error("Error unrestricting user:", error);
      showNotification(
        "error",
        "Error unrestricting user",
        "An error occurred while unrestricting the user. Please try again."
      );
    }
  };

  // AI Analysis handlers
  const handleAnalyzePost = async (postId) => {
    try {
      setAnalyzingPost(postId);
      const response = await analyzePost(postId);
      console.log("Analysis response:", response.data);
      
      // Check if response has error
      const analysisData = response.data;
      if (analysisData && analysisData.error) {
        console.error("AI analysis error:", analysisData.error);
        showNotification("error", "Analysis Failed", analysisData.error || "Failed to analyze the post. Please check AI service configuration.");
        setAnalyzingPost(null);
        return;
      }
      
      // Ensure summary is in correct format
      if (analysisData && !analysisData.summary) {
        // If summary is missing, set empty object structure
        analysisData.summary = {
          english: "",
          vietnamese: ""
        };
      } else if (analysisData && typeof analysisData.summary === 'string') {
        // Convert string summary to object format
        const summaryStr = analysisData.summary.trim();
        if (!summaryStr) {
          // If summary is empty string, set empty object
          analysisData.summary = {
            english: "",
            vietnamese: ""
          };
        } else {
          // If summary has content, use it as English summary
          analysisData.summary = {
            english: summaryStr,
            vietnamese: ""
          };
        }
      } else if (analysisData && analysisData.summary && typeof analysisData.summary === 'object') {
        // Ensure both english and vietnamese exist
        if (!analysisData.summary.english) analysisData.summary.english = "";
        if (!analysisData.summary.vietnamese) analysisData.summary.vietnamese = "";
      }
      
      setPostAnalysis(prev => ({
        ...prev,
        [postId]: analysisData
      }));
      showNotification("success", "Analysis Complete", "AI analysis has been completed successfully.");
    } catch (error) {
      console.error("Error analyzing post:", error);
      console.error("Error response:", error.response?.data);
      console.error("Error message:", error.message);
      showNotification("error", "Analysis Failed", error.response?.data?.error || error.response?.data?.message || "Failed to analyze the post. Please try again.");
    } finally {
      setAnalyzingPost(null);
    }
  };


  // Helper function to highlight inappropriate words in content
  const renderContentWithHighlights = (content, analysis) => {
    if (!content) return null;
    
    if (!analysis || !analysis.has_inappropriate_content || !analysis.inappropriate_words || analysis.inappropriate_words.length === 0) {
      return renderContent(content);
    }

    // Create array of segments (normal text and highlighted text)
    const segments = [];
    let lastIndex = 0;
    
    // Sort inappropriate words by start_index
    const sortedWords = [...analysis.inappropriate_words].sort((a, b) => 
      (a.start_index || 0) - (b.start_index || 0)
    );

    sortedWords.forEach((word) => {
      const start = word.start_index || 0;
      const end = word.end_index || content.length;
      
      // Add text before highlighted part
      if (start > lastIndex) {
        segments.push({
          text: content.substring(lastIndex, start),
          isHighlighted: false
        });
      }
      
      // Add highlighted part
      segments.push({
        text: content.substring(start, end),
        isHighlighted: true,
        type: word.type
      });
      
      lastIndex = end;
    });

    // Add remaining text
    if (lastIndex < content.length) {
      segments.push({
        text: content.substring(lastIndex),
        isHighlighted: false
      });
    }

    // Render segments with markdown parsing
    return (
      <div>
        {segments.map((segment, idx) => {
          if (segment.isHighlighted) {
            // Parse markdown for highlighted segment
            let html = segment.text;
            try {
              html = marked.parse(segment.text, {
                breaks: true,
                gfm: true
              });
            } catch (e) {
              // If markdown parsing fails, use plain text
            }
            return (
              <mark
                key={idx}
                style={{
                  backgroundColor: '#ffcccc',
                  color: '#cc0000',
                  fontWeight: 'bold',
                  padding: '2px 4px',
                  borderRadius: '3px',
                  textDecoration: 'underline',
                  textDecorationColor: '#cc0000'
                }}
                title={`Inappropriate content: ${segment.type}`}
                dangerouslySetInnerHTML={{ __html: html }}
              />
            );
          } else {
            // Parse markdown for normal segment
            try {
              const html = marked.parse(segment.text, {
                breaks: true,
                gfm: true
              });
              return <span key={idx} dangerouslySetInnerHTML={{ __html: html }} />;
            } catch (error) {
              return <span key={idx}>{segment.text}</span>;
            }
          }
        })}
      </div>
    );
  };

  const renderContent = (content) => {
    if (!content) return null;
    
    try {
      const html = marked.parse(content, {
        breaks: true,
        gfm: true
      });
      return <div dangerouslySetInnerHTML={{ __html: html }} />;
    } catch (error) {
      console.error("Error parsing markdown:", error);
      return <div>{content}</div>;
    }
  };

  const chartConfig = {
    labels: chartData.map(item => item.label || item.day),
    datasets: [
      {
        label: "Posts per day",
        data: chartData.map(item => item.value || item.posts),
        borderColor: "#007bff",
        backgroundColor: "rgba(0, 123, 255, 0.1)",
        tension: 0.4,
        fill: true,
        pointRadius: 5,
        pointHoverRadius: 7,
        borderWidth: 3,
      },
    ],
  };

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: true,
        position: 'top',
        labels: {
          font: {
            size: 14,
            weight: 'bold'
          }
        }
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        titleFont: {
          size: 16
        },
        bodyFont: {
          size: 14
        }
      }
    },
    scales: {
      x: {
        grid: {
          display: false
        },
        ticks: {
          font: {
            size: 12
          }
        }
      },
      y: {
        beginAtZero: true,
        grid: {
          color: 'rgba(0, 0, 0, 0.1)'
        },
        ticks: {
          font: {
            size: 12
          },
          stepSize: 1,
          callback: function(value) {
            if (Number.isInteger(value)) {
              return value;
            }
            return '';
          }
        }
      }
    }
  };

  const renderOverview = () => (
    <div className="moderator-content">
      <div className="posts-header">
        <h1 className="page-title">All Forum Posts</h1>
        
        <div className="posts-controls">
          <div className="search-box">
            <Search size={20} />
            <input
              type="text"
              placeholder="Search posts by title..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>
          
          <div className="filter-select">
            <Filter size={16} />
            <select 
              value={selectedTag} 
              onChange={(e) => setSelectedTag(e.target.value)}
            >
              <option value="all">All Tags</option>
              {tags.map(tag => (
                <option key={tag.tagId} value={tag.tagId}>
                  #{tag.tagName}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {postsLoading ? (
        <div className="loading">Loading posts...</div>
      ) : (
        <div className="posts-list">
          {allPosts.length === 0 ? (
            <div className="no-posts">No posts found</div>
          ) : (
            allPosts.map(post => (
              <div key={post.postId} className="post-card">
                <div className="post-header">
                  <h3>{post.title}</h3>
                  <span className={`post-status ${post.status}`}>
                    {post.status === 'pending' ? 'Pending' : 
                     post.status === 'rejected' ? 'Rejected' : 
                     post.status === 'approved' ? 'Approved' : post.status}
                  </span>
                </div>
                
                <div className="post-meta">
                  <div className="post-author">
                    <User size={16} />
                    <span>@{post.user?.username || 'Unknown'}</span>
                  </div>
                  <div className="post-time">
                    <Clock size={16} />
                    <span>{formatTimeVietnam(post.createdAt)}</span>
                  </div>
                  <div className="post-stats">
                    <ThumbsUp size={16} />
                    <span>{post.voteCount || 0}</span>
                    <MessageSquare size={16} />
                    <span>{post.commentCount || 0}</span>
                    <Eye size={16} />
                    <span>{post.viewCount || 0}</span>
                  </div>
                </div>

                <div className="post-content">
                  {post.content && post.content.length > 200 
                    ? `${post.content.substring(0, 200)}...` 
                    : post.content || ''}
                </div>

                {post.tags && post.tags.length > 0 && (
                  <div className="post-tags">
                    {post.tags.map((tag, index) => (
                      <span key={index} className="post-tag">
                        #{tag.tagName}
                      </span>
                    ))}
                  </div>
                )}

                <div className="post-actions">
                  <button 
                    className="btn btn-primary"
                    onClick={() => handleViewPost(post)}
                  >
                    <Eye size={16} />
                    View Detail
                  </button>
                  
                  {post.status === 'pending' && (
                    <>
                      <button 
                        className="btn btn-success"
                        onClick={() => handleAcceptPost(post.postId)}
                      >
                        <Check size={16} />
                        Approve
                      </button>
                      <button 
                        className="btn btn-danger"
                        onClick={() => handleOpenRejectPopup(post.postId)}
                      >
                        <X size={16} />
                        Reject
                      </button>
                    </>
                  )}
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );

  const renderPendingPosts = () => (
    <div className="moderator-content">
      <h1 className="page-title">Pending Posts</h1>
      <div className="posts-list">
        {pendingPosts.map(post => (
          <div key={post.id} className="post-card">
            <div className="post-header">
              <h3>{post.title}</h3>
              <span className="post-status pending">Pending</span>
            </div>
            <div className="post-meta">
              <span>By: {post.user?.username || 'Unknown'}</span>
              <span>{formatTimeVietnam(post.createdAt)}</span>
            </div>
            <div className="post-content">
              {post.content.substring(0, 200)}...
            </div>
            <div className="post-actions">
              <button 
                className="btn btn-primary"
                onClick={() => handleViewPost(post, true)}
              >
                <Eye size={16} />
                View Detail
              </button>
              <button 
                className="btn btn-success"
                onClick={() => handleAcceptPost(post.postId || post.id)}
              >
                <Check size={16} />
                Accept
              </button>
                <button 
                className="btn btn-danger"
                onClick={() => handleOpenRejectPopup(post.postId || post.id)}
              >
                <X size={16} />
                Reject
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderReportedComments = () => (
    <div className="moderator-content">
      <h1 className="page-title">Reported Comments</h1>
      <div className="posts-list">
        {reportedComments.map(comment => (
          <div key={comment.reportId} className="post-card">
            <div className="post-header">
              <h3>Comment on: {comment.postTitle}</h3>
              <span className="post-status reported">Reported ({comment.reportCount})</span>
            </div>
            <div className="post-meta">
              <span>By: {comment.author || 'Unknown'}</span>
              <span>{formatTimeVietnam(comment.createdAt)}</span>
            </div>
            <div className="post-content">
              {comment.content.substring(0, 200)}...
            </div>
            <div className="report-reason">
              <strong>Report Reason:</strong> {comment.reportReason}
            </div>
            <div className="post-actions">
              <button 
                className="btn btn-primary"
                onClick={() => handleViewComment(comment)}
              >
                <Eye size={16} />
                View Detail
              </button>
              <button 
                className="btn btn-success"
                onClick={() => handleApproveReport(comment.reportId)}
                title="Approve report and delete comment"
              >
                <Check size={16} />
                Approve & Delete
              </button>
              <button 
                className="btn btn-secondary"
                onClick={() => handleDismissReport(comment.reportId)}
                title="Dismiss report and keep comment"
              >
                <X size={16} />
                Dismiss
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderRejectedPosts = () => (
    <div className="moderator-content">
      <h1 className="page-title">Rejected Posts</h1>
      <div className="posts-list">
        {rejectedPosts.map(post => (
          <div key={post.postId} className="post-card">
            <div className="post-header">
              <h3>{post.title}</h3>
              <span className="post-status rejected">Rejected</span>
            </div>
            <div className="post-meta">
              <span>Author: {post.user?.username || 'Unknown'}</span>
              <span>Date: {formatTimeVietnam(post.createdAt)}</span>
              {post.rejectionReason && (
                <span className="rejection-reason">Reason: {post.rejectionReason}</span>
              )}
            </div>
            <div className="post-content">
              {post.content.substring(0, 200)}...
            </div>
            <div className="post-actions">
              <button 
                className="btn btn-primary"
                onClick={() => handleViewPost(post)}
              >
                <Eye size={16} />
                View Detail
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );

  const renderStatistics = () => (
    <div className="moderator-content">
      <h1 className="page-title">Statistics Dashboard</h1>
      
      {/* Stats Cards */}
      <div className="stats-grid">
        <div className="stat-card total">
          <div className="stat-icon">
            <FileText size={24} />
          </div>
          <div className="stat-content">
            <h3>Total Posts</h3>
            <p className="stat-number">{stats.total}</p>
          </div>
        </div>

        <div className="stat-card pending">
          <div className="stat-icon">
            <Clock size={24} />
          </div>
          <div className="stat-content">
            <h3>Pending</h3>
            <p className="stat-number">{stats.pending}</p>
          </div>
        </div>

        <div className="stat-card reported">
          <div className="stat-icon">
            <Flag size={24} />
          </div>
          <div className="stat-content">
            <h3>Reported</h3>
            <p className="stat-number">{stats.reported}</p>
          </div>
        </div>

        <div className="stat-card rejected">
          <div className="stat-icon">
            <XCircle size={24} />
          </div>
          <div className="stat-content">
            <h3>Rejected</h3>
            <p className="stat-number">{stats.rejected}</p>
          </div>
        </div>
      </div>

      {/* Chart Section */}
      <div className="chart-section">
        <div className="chart-header">
          <h2>Posts Statistics</h2>
          <div className="month-selector">
            <Calendar size={16} />
            <select 
              value={selectedMonth} 
              onChange={(e) => setSelectedMonth(parseInt(e.target.value))}
            >
              <option value={1}>January</option>
              <option value={2}>February</option>
              <option value={3}>March</option>
              <option value={4}>April</option>
              <option value={5}>May</option>
              <option value={6}>June</option>
              <option value={7}>July</option>
              <option value={8}>August</option>
              <option value={9}>September</option>
              <option value={10}>October</option>
              <option value={11}>November</option>
              <option value={12}>December</option>
            </select>
          </div>
        </div>
        <div className="chart-container">
          <Line data={chartConfig} options={chartOptions} />
        </div>
      </div>

      {/* Users Table */}
      <div className="users-section">
        <h2>Users Management</h2>
        <div className="users-table">
          <table>
            <thead>
              <tr>
                <th>User</th>
                <th>Posts</th>
                <th>Comments</th>
                <th>Approved</th>
                <th>Rejected Post</th>
                <th>Reported Comment</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map(user => (
                <tr key={user.userId || user.id}>
                  <td>
                    <div className="user-info">
                      <User size={16} />
                      <div>
                        <div className="username">{user.username}</div>
                        <div className="email">{user.email}</div>
                      </div>
                    </div>
                  </td>
                  <td>{user.totalPosts || user.posts || 0}</td>
                  <td>{user.totalComments || user.comments || 0}</td>
                  <td><span className="approved">{user.approvedPosts || user.approved || 0}</span></td>
                  <td><span className="rejected">{user.rejectedPosts || user.rejected || 0}</span></td>
                  <td><span className="reported">{user.reportedComments || user.reported || 0}</span></td>
                  <td>
                    {user.isRestricted ? (
                      <button 
                        className="btn-action unrestrict"
                        onClick={() => handleUnrestrictUser(user.userId || user.id)}
                        title="Remove restriction from this user"
                      >
                        Unrestrict
                      </button>
                    ) : (
                      <button 
                        className="btn-action restrict"
                        onClick={() => handleRestrictUser(user.userId || user.id)}
                        title="Restrict this user from posting and commenting"
                      >
                        Restrict
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );

  // Tag management functions
  const loadTags = async () => {
    try {
      setTagsLoading(true);
      const response = await getAllTags();
      setTags(response);
    } catch (error) {
      console.error('Error loading tags:', error);
      setTagError('Failed to load tags');
    } finally {
      setTagsLoading(false);
    }
  };

  const handleCreateTag = async (e) => {
    e.preventDefault();
    if (!tagName.trim()) {
      setTagError('Tag name is required');
      return;
    }

    try {
      await createTag({ tagName: tagName.trim() });
      setTagName('');
      setShowTagModal(false);
      setTagError('');
      loadTags();
    } catch (error) {
      console.error('Error creating tag:', error);
      setTagError(error.response?.data?.message || 'Failed to create tag');
    }
  };

  const handleEditTag = async (e) => {
    e.preventDefault();
    if (!tagName.trim()) {
      setTagError('Tag name is required');
      return;
    }

    try {
      await updateTag(editingTag.tagId, { tagName: tagName.trim() });
      setTagName('');
      setEditingTag(null);
      setShowTagModal(false);
      setTagError('');
      loadTags();
    } catch (error) {
      console.error('Error updating tag:', error);
      setTagError(error.response?.data?.message || 'Failed to update tag');
    }
  };

  const handleDeleteTag = async (tagId) => {
    if (!window.confirm('Are you sure you want to delete this tag?')) {
      return;
    }

    try {
      await deleteTag(tagId);
      loadTags();
    } catch (error) {
      console.error('Error deleting tag:', error);
      setTagError('Failed to delete tag');
    }
  };

  const openEditModal = (tag) => {
    setEditingTag(tag);
    setTagName(tag.tagName);
    setShowTagModal(true);
    setTagError('');
  };

  const closeModal = () => {
    setShowTagModal(false);
    setEditingTag(null);
    setTagName('');
    setTagError('');
  };

  const renderTags = () => (
    <div className="moderator-content">
      <div className="tag-header">
        <h1>Tag Management</h1>
        <button 
          className="btn-primary"
          onClick={() => setShowTagModal(true)}
        >
          <Plus size={20} />
          Create Tag
        </button>
      </div>

      {tagError && (
        <div className="error-message">
          {tagError}
        </div>
      )}

      {tagsLoading ? (
        <div className="loading">Loading tags...</div>
      ) : (
        <div className="tags-list">
          {tags.length === 0 ? (
            <div className="no-tags">
              <p>No tags found. Create your first tag!</p>
            </div>
          ) : (
            tags.map(tag => (
              <div key={tag.tagId} className="tag-card">
                <div className="tag-info">
                  <span className="tag-name">#{tag.tagName}</span>
                  <span className="tag-count">{tag.postCount || 0} posts</span>
                </div>
                <div className="tag-actions">
                  <button 
                    className="btn-edit"
                    onClick={() => openEditModal(tag)}
                  >
                    <Edit size={16} />
                  </button>
                  <button 
                    className="btn-delete"
                    onClick={() => handleDeleteTag(tag.tagId)}
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      )}

      {/* Tag Modal */}
      {showTagModal && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h2>{editingTag ? 'Edit Tag' : 'Create Tag'}</h2>
              <button className="close-btn" onClick={closeModal}>
                <X size={20} />
              </button>
            </div>
            <form onSubmit={editingTag ? handleEditTag : handleCreateTag}>
              <div className="modal-body">
                <div className="form-group">
                  <label>Tag Name</label>
                  <input
                    type="text"
                    value={tagName}
                    onChange={(e) => setTagName(e.target.value)}
                    placeholder="Enter tag name"
                    required
                  />
                </div>
                {tagError && (
                  <div className="error-message">{tagError}</div>
                )}
              </div>
              <div className="modal-footer">
                <button type="button" onClick={closeModal} className="btn-secondary">
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  {editingTag ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );

  const renderNotifications = () => (
    <div className="moderator-content">
      <h1 className="page-title">Notifications</h1>
      <div className="notifications-list">
        <div className="notification-item">
          <Bell size={20} />
          <div className="notification-content">
            <h4>New pending post</h4>
            <p>john_doe submitted a new post for review</p>
            <span className="notification-time">2 hours ago</span>
          </div>
        </div>
        <div className="notification-item">
          <Flag size={20} />
          <div className="notification-content">
            <h4>Comment reported</h4>
            <p>User reported a comment for inappropriate content</p>
            <span className="notification-time">4 hours ago</span>
          </div>
        </div>
      </div>
    </div>
  );

  return (
    <div className="moderator-dashboard">
      <AppLayout 
        title="Moderator Dashboard" 
        sidebar={<ModeratorNavbar currentView={currentView} onViewChange={setCurrentView} />}
      >
        <div className="moderator-main">
          {currentView === "overview" && renderOverview()}
          {currentView === "statistics" && renderStatistics()}
          {currentView === "tags" && renderTags()}
          {currentView === "pending" && renderPendingPosts()}
          {currentView === "reported" && renderReportedComments()}
          {currentView === "rejected" && renderRejectedPosts()}
        </div>
      </AppLayout>

      {/* Post Detail Modal */}
      {showPostDetail && selectedPost && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h2>{selectedPost.title}</h2>
              <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
                <button 
                  className="close-btn"
                  onClick={() => setShowPostDetail(false)}
                >
                  <XCircle size={20} />
                </button>
              </div>
            </div>
            
            <div className="modal-body">
              <div className="post-meta">
                <div className="post-author">
                  <User size={16} />
                  <span>Author: {selectedPost.user?.username || selectedPost.author || 'Unknown'}</span>
                </div>
                <div className="post-time">
                  <Clock size={16} />
                  <span>Date: {formatTimeVietnam(selectedPost.createdAt)}</span>
                </div>
                {selectedPost.viewCount !== undefined && (
                  <div className="post-stats">
                    <Eye size={16} />
                    <span>{selectedPost.viewCount} views</span>
                  </div>
                )}
              </div>

              {selectedPost.tags && selectedPost.tags.length > 0 && (
                <div className="post-tags-detail">
                  {selectedPost.tags.map((tag, index) => (
                    <span key={index} className="post-tag-detail">
                      #{tag.tagName}
                    </span>
                  ))}
                </div>
              )}

              <div className="post-content-full" style={{ 
                lineHeight: '1.6',
                fontSize: '16px',
                color: '#333'
              }}>
                {postAnalysis[selectedPost.postId || selectedPost.id] 
                  ? renderContentWithHighlights(selectedPost.content, postAnalysis[selectedPost.postId || selectedPost.id])
                  : renderContent(selectedPost.content)
                }
              </div>

              {selectedPost.reportReason && (
                <div className="report-info">
                  <h4>Report Reason:</h4>
                  <p>{selectedPost.reportReason}</p>
                </div>
              )}

              {/* Comments Section */}
              <div style={{ marginTop: '30px', paddingTop: '20px', borderTop: '1px solid #e9ecef' }}>
                <h4 style={{ marginBottom: '20px' }}>Comments</h4>
                <CommentSection 
                  postId={selectedPost.postId || selectedPost.id} 
                  postOwnerId={selectedPost.user?.userId}
                />
              </div>

              {/* AI Analysis Section - At the end of modal body */}
              {postAnalysis[selectedPost.postId || selectedPost.id] && (
                <div style={{ 
                  marginTop: '30px', 
                  padding: '20px', 
                  background: '#f8f9fa', 
                  borderRadius: '8px', 
                  border: '1px solid #dee2e6' 
                }}>
                  <h4 style={{ marginBottom: '15px', display: 'flex', alignItems: 'center', gap: '8px', color: '#212529' }}>
                    <Sparkles size={20} color="#007bff" />
                    AI Analysis
                  </h4>
                  
                  {/* Meaningfulness Check */}
                  {postAnalysis[selectedPost.postId || selectedPost.id].is_meaningful !== undefined && (
                    <div style={{ 
                      marginBottom: '15px', 
                      padding: '12px', 
                      background: postAnalysis[selectedPost.postId || selectedPost.id].is_meaningful ? '#d4edda' : '#f8d7da',
                      borderRadius: '6px', 
                      border: `1px solid ${postAnalysis[selectedPost.postId || selectedPost.id].is_meaningful ? '#28a745' : '#dc3545'}`,
                      color: postAnalysis[selectedPost.postId || selectedPost.id].is_meaningful ? '#155724' : '#721c24'
                    }}>
                      <strong style={{ display: 'block', marginBottom: '5px' }}>
                        {postAnalysis[selectedPost.postId || selectedPost.id].is_meaningful ? '✓ Content is Meaningful' : '⚠ Content May Not Be Meaningful'}
                      </strong>
                      {postAnalysis[selectedPost.postId || selectedPost.id].meaningfulness_reason && (
                        <p style={{ margin: 0, fontSize: '14px' }}>
                          {postAnalysis[selectedPost.postId || selectedPost.id].meaningfulness_reason}
                        </p>
                      )}
                    </div>
                  )}

                  {/* Error Display */}
                  {postAnalysis[selectedPost.postId || selectedPost.id].error && (
                    <div style={{ 
                      marginBottom: '15px', 
                      padding: '12px', 
                      background: '#f8d7da', 
                      borderRadius: '6px', 
                      border: '1px solid #dc3545',
                      color: '#721c24'
                    }}>
                      <strong style={{ display: 'block', marginBottom: '5px' }}>⚠ Error:</strong>
                      <p style={{ margin: 0, fontSize: '14px' }}>
                        {postAnalysis[selectedPost.postId || selectedPost.id].error}
                      </p>
                    </div>
                  )}

                  {/* Bilingual Summary */}
                  {!postAnalysis[selectedPost.postId || selectedPost.id].error && (
                    <div style={{ marginBottom: '15px' }}>
                      <strong style={{ color: '#495057', display: 'block', marginBottom: '10px' }}>Summary:</strong>
                      {postAnalysis[selectedPost.postId || selectedPost.id].summary ? (
                        typeof postAnalysis[selectedPost.postId || selectedPost.id].summary === 'object' ? (
                          <>
                            {(postAnalysis[selectedPost.postId || selectedPost.id].summary.english && postAnalysis[selectedPost.postId || selectedPost.id].summary.english.trim()) || (postAnalysis[selectedPost.postId || selectedPost.id].summary.vietnamese && postAnalysis[selectedPost.postId || selectedPost.id].summary.vietnamese.trim()) ? (
                              <>
                                {postAnalysis[selectedPost.postId || selectedPost.id].summary.english && postAnalysis[selectedPost.postId || selectedPost.id].summary.english.trim() && (
                                  <div style={{ marginBottom: '12px' }}>
                                    <strong style={{ color: '#495057', display: 'block', marginBottom: '5px', fontSize: '14px' }}>English:</strong>
                                    <p style={{ margin: 0, color: '#495057', lineHeight: '1.6', fontSize: '14px' }}>
                                      {postAnalysis[selectedPost.postId || selectedPost.id].summary.english}
                                    </p>
                                  </div>
                                )}
                                {postAnalysis[selectedPost.postId || selectedPost.id].summary.vietnamese && postAnalysis[selectedPost.postId || selectedPost.id].summary.vietnamese.trim() && (
                                  <div>
                                    <strong style={{ color: '#495057', display: 'block', marginBottom: '5px', fontSize: '14px' }}>Tiếng Việt:</strong>
                                    <p style={{ margin: 0, color: '#495057', lineHeight: '1.6', fontSize: '14px' }}>
                                      {postAnalysis[selectedPost.postId || selectedPost.id].summary.vietnamese}
                                    </p>
                                  </div>
                                )}
                              </>
                            ) : (
                              <p style={{ margin: 0, color: '#6c757d', fontStyle: 'italic' }}>No summary available</p>
                            )}
                          </>
                        ) : (
                          postAnalysis[selectedPost.postId || selectedPost.id].summary.trim() ? (
                            <p style={{ margin: 0, color: '#495057', lineHeight: '1.6' }}>
                              {postAnalysis[selectedPost.postId || selectedPost.id].summary}
                            </p>
                          ) : (
                            <p style={{ margin: 0, color: '#6c757d', fontStyle: 'italic' }}>No summary available</p>
                          )
                        )
                      ) : (
                        <p style={{ margin: 0, color: '#6c757d', fontStyle: 'italic' }}>No summary available</p>
                      )}
                    </div>
                  )}
                  {postAnalysis[selectedPost.postId || selectedPost.id].has_inappropriate_content && (
                    <div style={{ 
                      marginTop: '15px', 
                      padding: '15px', 
                      background: '#fff3cd', 
                      borderRadius: '6px', 
                      border: '1px solid #ffc107' 
                    }}>
                      <strong style={{ color: '#cc0000', display: 'block', marginBottom: '10px' }}>
                        ⚠ Inappropriate Content Detected
                      </strong>
                      <p style={{ margin: '0 0 10px 0', fontSize: '14px', color: '#856404' }}>
                        Found {postAnalysis[selectedPost.postId || selectedPost.id].inappropriate_words?.length || 0} inappropriate word(s). Details below:
                      </p>
                      {postAnalysis[selectedPost.postId || selectedPost.id].inappropriate_words && 
                       postAnalysis[selectedPost.postId || selectedPost.id].inappropriate_words.length > 0 && (
                        <div style={{ marginTop: '10px' }}>
                          <ul style={{ margin: 0, paddingLeft: '20px', color: '#856404' }}>
                            {postAnalysis[selectedPost.postId || selectedPost.id].inappropriate_words.map((word, idx) => (
                              <li key={idx} style={{ marginBottom: '5px' }}>
                                <strong>"{word.text || word.excerpt || 'Unknown'}"</strong>
                                {word.type && <span style={{ marginLeft: '8px', fontSize: '12px' }}>({word.type})</span>}
                                {word.explanation && <div style={{ fontSize: '12px', marginTop: '3px', color: '#6c757d' }}>{word.explanation}</div>}
                              </li>
                            ))}
                          </ul>
                        </div>
                      )}
                      <p style={{ margin: '10px 0 0 0', fontSize: '13px', color: '#856404', fontStyle: 'italic' }}>
                        Inappropriate words are highlighted in red in the content above.
                      </p>
                    </div>
                  )}
                  {!postAnalysis[selectedPost.postId || selectedPost.id].has_inappropriate_content && (
                    <div style={{ 
                      marginTop: '15px', 
                      padding: '10px', 
                      background: '#d4edda', 
                      borderRadius: '6px', 
                      border: '1px solid #28a745',
                      color: '#155724'
                    }}>
                      ✓ No inappropriate content detected. Content appears to be appropriate.
                    </div>
                  )}
                </div>
              )}
            </div>

            <div className="modal-footer">
              <div style={{ display: 'flex', gap: '10px', alignItems: 'center', flexWrap: 'wrap' }}>
                {(selectedPost.status === "pending" || selectedPost.reportReason || currentView === "pending") && (
                  <button 
                    className="btn btn-info"
                    onClick={() => handleAnalyzePost(selectedPost.postId || selectedPost.id)}
                    disabled={analyzingPost === (selectedPost.postId || selectedPost.id)}
                    style={{ 
                      display: 'flex', 
                      alignItems: 'center', 
                      gap: '8px',
                      padding: '8px 16px'
                    }}
                  >
                    {analyzingPost === (selectedPost.postId || selectedPost.id) ? (
                      <>
                        <Loader size={16} className="spinning" style={{ animation: 'spin 1s linear infinite' }} />
                        Analyzing...
                      </>
                    ) : (
                      <>
                        <Sparkles size={16} />
                        AI Analyze
                      </>
                    )}
                  </button>
                )}
              </div>
              <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
                {selectedPost.status === "pending" ? (
                  <>
                    <button 
                      className="btn btn-success"
                      onClick={() => handleAcceptPost(selectedPost.id)}
                    >
                      <CheckCircle size={16} />
                      Accept
                    </button>
                    <button 
                      className="btn btn-danger"
                      onClick={() => handleOpenRejectPopup(selectedPost.id)}
                    >
                      <XCircle size={16} />
                      Reject
                    </button>
                  </>
                ) : (
                  <button 
                    className="btn btn-primary"
                    onClick={() => setShowPostDetail(false)}
                  >
                    Close
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Comment Detail Modal */}
      {showCommentDetail && selectedComment && (
        <div className="modal-overlay">
          <div className="modal-content">
            <div className="modal-header">
              <h2>Reported Comment</h2>
              <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
                <button 
                  className="close-btn"
                  onClick={() => setShowCommentDetail(false)}
                >
                  <XCircle size={20} />
                </button>
              </div>
            </div>
            
            <div className="modal-body">
              <div className="post-meta">
                <div className="post-author">
                  <User size={16} />
                  <span>Author: {selectedComment.author || 'Unknown'}</span>
                </div>
                <div className="post-time">
                  <Clock size={16} />
                  <span>Date: {formatTimeVietnam(selectedComment.createdAt)}</span>
                </div>
              </div>
              
              <div className="post-title-detail">
                <h3>Comment on: {selectedComment.postTitle}</h3>
              </div>

              <div className="post-content-full" style={{ 
                lineHeight: '1.6',
                fontSize: '16px',
                color: '#333',
                padding: '20px',
                backgroundColor: '#f8f9fa',
                borderRadius: '8px',
                marginTop: '20px',
                whiteSpace: 'pre-wrap',
                wordWrap: 'break-word'
              }}>
                {renderContent(selectedComment.content)}
              </div>

              {selectedComment.reportReason && (
                <div className="report-info" style={{ 
                  marginTop: '20px',
                  padding: '15px',
                  backgroundColor: '#fff3cd',
                  border: '1px solid #ffc107',
                  borderRadius: '8px'
                }}>
                  <h4>Report Reason:</h4>
                  <p>{selectedComment.reportReason}</p>
                </div>
              )}
            </div>

            <div className="modal-footer">
              <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
                <button 
                  className="btn btn-success"
                  onClick={() => {
                    handleApproveReport(selectedComment.reportId);
                    setShowCommentDetail(false);
                  }}
                >
                  <Check size={16} />
                  Approve & Delete
                </button>
                <button 
                  className="btn btn-secondary"
                  onClick={() => {
                    handleDismissReport(selectedComment.reportId);
                    setShowCommentDetail(false);
                  }}
                >
                  <X size={16} />
                  Dismiss
                </button>
                <button 
                  className="btn btn-primary"
                  onClick={() => setShowCommentDetail(false)}
                >
                  Close
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      <NotificationPopup
        isOpen={notification.isOpen}
        onClose={closeNotification}
        type={notification.type}
        title={notification.title}
        message={notification.message}
      />

      <RejectionReasonPopup
        isOpen={showRejectionPopup}
        onClose={() => {
          setShowRejectionPopup(false);
          setPostToReject(null);
        }}
        onConfirm={handleRejectPost}
        title="Reject Post"
      />
    </div>
  );
}
