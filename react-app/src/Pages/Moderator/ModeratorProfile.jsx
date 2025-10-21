import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import ModeratorSidebar from "../../Components/Moderator/ModeratorSidebar";
import { getMe, logout } from "../../Services/AuthApi";
import { User, LogOut, LogIn, Shield } from "lucide-react";
import "./ModeratorProfile.css";

// Import profile tabs
import ProfileTab from "../Profile/Tabs/ProfileTab";
import SignInTab from "../Profile/Tabs/SignInTab";

export default function ModeratorProfile() {
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  const [showUserMenu, setShowUserMenu] = useState(false);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState("profile");
  const [profileData, setProfileData] = useState({
    name: "",
    gmail: "",
    accountName: "",
    password: "",
    avatar: "",
  });


  useEffect(() => {
    loadUserData();
  }, []);

  useEffect(() => {
    if (user) {
      setProfileData({
        name: `${user.firstname || ""} ${user.lastname || ""}`.trim(),
        gmail: user.email || "",
        accountName: user.username || "",
        password: "",
        avatar: user.avatar || "",
      });
    }
  }, [user]);

  const loadUserData = async () => {
    try {
      const response = await getMe();
      setUser(response.data);
    } catch (error) {
      console.error('Error loading user data:', error);
      navigate('/login');
    } finally {
      setLoading(false);
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

  const renderContent = () => {
    switch (activeTab) {
      case "profile":
        return (
          <ProfileTab
            user={user}
            refreshUser={() => loadUserData()}
            profileData={profileData}
            setProfileData={setProfileData}
          />
        );
      case "sign-in-history":
        return <SignInTab />;
      default:
        return null;
    }
  };

  if (loading) {
    return (
      <div className="moderator-profile">
        <div className="loading">Loading profile...</div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="moderator-profile">
        <div className="loading">Please login</div>
      </div>
    );
  }

  return (
    <div className="moderator-profile">
      {/* Header Bar */}
      <header className="moderator-header">
        <div className="header-left">
          <h1>Moderator Profile</h1>
        </div>
        <div className="header-right">
          <div className="user-menu">
            <button 
              className="user-button"
              onClick={() => setShowUserMenu(!showUserMenu)}
            >
              <User size={20} />
              <span>{user?.username || 'Moderator'}</span>
            </button>
            {showUserMenu && (
              <div className="user-dropdown">
                <button 
                  className="dropdown-item"
                  onClick={() => navigate('/moderator/dashboard')}
                >
                  <User size={16} />
                  Dashboard
                </button>
                <button 
                  className="dropdown-item"
                  onClick={handleLogout}
                >
                  <LogOut size={16} />
                  Logout
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      <div className="moderator-content-wrapper">
        <ModeratorSidebar 
          currentView="profile" 
          onViewChange={(view) => {
            if (view === 'profile') return;
            navigate('/moderator/dashboard');
          }}
        />
        
        <main className="moderator-main">
          <div className="profile-layout">
            <div className="profile-sidebar">
              <div className="user-info">
                <div className="user-avatar">
                  {profileData.avatar ? (
                    <img
                      src={profileData.avatar}
                      alt="User Avatar"
                      className="sidebar-avatar-image"
                    />
                  ) : (
                    <User size={60} />
                  )}
                </div>
                <div className="user-details">
                  <h3>{profileData.name || "..."}</h3>
                  <p>{profileData.gmail || "..."}</p>
                  <div className="role-badge">
                    <Shield size={14} />
                    {user?.role || 'Moderator'}
                  </div>
                </div>
              </div>

              <div className="navigation-menu">
                {[
                  {
                    key: "profile",
                    icon: <User size={20} />,
                    label: "Your Profile",
                  },
                  {
                    key: "sign-in-history",
                    icon: <LogIn size={20} />,
                    label: "Sign In History",
                  },
                ].map(({ key, icon, label }) => (
                  <button
                    key={key}
                    className={`nav-item ${activeTab === key ? "active" : ""}`}
                    onClick={() => setActiveTab(key)}
                  >
                    {icon}
                    <span>{label}</span>
                  </button>
                ))}
              </div>
            </div>

            <div className="profile-main">{renderContent()}</div>
          </div>
        </main>
      </div>
    </div>
  );
}
