import React, { useState, useEffect } from "react";
import { User, CreditCard, LogIn } from "lucide-react";
import "./Profile.css";
import useAuth from "../../Hook/UseAuth";
import AppLayout from "../../Components/Layout/AppLayout";

import ProfileTab from "./Tabs/ProfileTab";
import PaymentTab from "./Tabs/PaymentTab";
import SignInTab from "./Tabs/SignInTab";
import { useLocation } from "react-router-dom";

export default function Profile() {
  const { user, loading, refreshUser } = useAuth();
  const [activeTab, setActiveTab] = useState("profile");
  const [profileData, setProfileData] = useState({
    name: "",
    gmail: "",
    accountName: "",
    avatar: "",
  });

  const location = useLocation();

  // handle tab navigation from router state
  useEffect(() => {
    if (location.state?.activeTab) {
      setActiveTab(location.state.activeTab);
    }
  }, [location.state]);

  // load user info
  useEffect(() => {
    if (user) {
      setProfileData({
        name: `${user.firstname || ""} ${user.lastname || ""}`.trim(),
        gmail: user.email || "",
        accountName: user.username || "",
        avatar: user.avatar || "",
      });
    }
  }, [user]);

  if (loading)
    return (
      <AppLayout title="Profile">
        <div className="profile-center">Loading...</div>
      </AppLayout>
    );

  if (!user)
    return (
      <AppLayout title="Profile">
        <div className="profile-center">Please login to continue</div>
      </AppLayout>
    );

  const renderContent = () => {
    switch (activeTab) {
      case "profile":
        return (
          <ProfileTab
            user={user}
            refreshUser={refreshUser}
            profileData={profileData}
            setProfileData={setProfileData}
          />
        );

      case "payment-history":
        return <PaymentTab />;

      case "sign-in-history":
        return <SignInTab />;

      default:
        return null;
    }
  };

  return (
    <AppLayout title="Profile">
      <div className="profile-page">
        {/* Sidebar */}
        <aside className="profile-sidebar">
          <div className="profile-user">
            <div className="avatar-box">
              {profileData.avatar ? (
                <img
                  src={profileData.avatar}
                  alt="avatar"
                  className="avatar-img"
                />
              ) : (
                <User size={60} strokeWidth={1.5} />
              )}
            </div>
            <h3>{profileData.name || "..."}</h3>
            <p>{profileData.gmail || "..."}</p>
          </div>

          <div className="profile-nav">
            {[
              {
                key: "profile",
                icon: <User size={18} />,
                label: "Your Profile",
              },
              {
                key: "payment-history",
                icon: <CreditCard size={18} />,
                label: "Payment History",
              },
              {
                key: "sign-in-history",
                icon: <LogIn size={18} />,
                label: "Sign In History",
              },
            ].map(({ key, icon, label }) => (
              <button
                key={key}
                onClick={() => setActiveTab(key)}
                className={`nav-item ${activeTab === key ? "active" : ""}`}
              >
                {icon}
                <span>{label}</span>
              </button>
            ))}
          </div>
        </aside>

        {/* Main content */}
        <main className="profile-main">{renderContent()}</main>
      </div>
    </AppLayout>
  );
}
