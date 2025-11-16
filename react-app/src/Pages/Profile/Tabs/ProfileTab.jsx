import React, { useState } from "react";
import { User } from "lucide-react";
import { compressAndUploadImage } from "../../../utils/ImageHelper";
import { updateUser } from "../../../Services/UserApi";
import ChangePasswordModal from "../../../Components/Auth/ChangePasswordModal";
import InputField from "../../../Components/Auth/InputField";
import "./ProfileTab.css";

export default function ProfileTab({ user, profileData, setProfileData }) {
  const [isSaving, setIsSaving] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);
  const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);

  // ===== Handle input changes =====
  const handleChange = (e) => {
    const { name, value } = e.target;
    setProfileData((prev) => ({ ...prev, [name]: value }));
    setHasChanges(true);
  };

  // ===== Handle avatar upload =====
  const handleAvatarUpload = (event) => {
    const file = event.target.files[0];
    if (!file) return;

    setIsUploadingAvatar(true);
    compressAndUploadImage(file)
      .then((url) => {
        setProfileData((prev) => ({ ...prev, avatar: url }));
        setHasChanges(true);
      })
      .catch((err) => alert(err.message || "Failed to upload image"))
      .finally(() => setIsUploadingAvatar(false));
  };

  // ===== Save profile changes =====
  const handleSave = () => {
    if (!user) return;

    if (!profileData.name.trim()) {
      alert("Name cannot be empty!");
      return;
    }

    if (!profileData.accountName.trim()) {
      alert("Username cannot be empty!");
      return;
    }

    setIsSaving(true);

    updateUser(user.userId, {
      firstname: profileData.name.split(" ")[0],
      lastname: profileData.name.split(" ").slice(1).join(" "),

      username: profileData.accountName,
      password: profileData.password || undefined,
      avatar: profileData.avatar === "" ? null : profileData.avatar,
    })
      .then(() => {
        alert("Profile updated successfully!");
        window.location.reload();
      })
      .catch((err) => {
        console.error("Error saving profile:", err);
        alert("Failed to update profile. Please try again.");
      })
      .finally(() => setIsSaving(false));
  };

  return (
    <div className="profile-content">
      <h2>Your Profile</h2>

      <div className="profile-form">
        {/* ===== Name ===== */}
        <div className="form-group">
          <label>Name</label>
          <InputField
            name="name"
            type="text"
            placeholder="Enter your name"
            value={profileData.name}
            onChange={handleChange}
          />
        </div>

        {/* ===== Gmail ===== */}
        <div className="form-group">
          <label>Gmail</label>
          <InputField
            name="gmail"
            type="email"
            value={profileData.gmail}
            disabled
          />
        </div>

        {/* ===== Account Name ===== */}
        <div className="form-group">
          <label>Account Name</label>
          <InputField
            name="accountName"
            type="text"
            placeholder="Enter your account name"
            value={profileData.accountName}
            onChange={handleChange}
          />
        </div>

        {/* ===== Avatar ===== */}
        <div className="form-group">
          <label>Avatar</label>
          <div className="avatar-upload-section">
            <div className="avatar-preview">
              {profileData.avatar ? (
                <img
                  src={profileData.avatar}
                  alt="Avatar"
                  className="avatar-image"
                />
              ) : (
                <div className="avatar-placeholder">
                  <User size={40} />
                </div>
              )}
            </div>

            <div className="avatar-upload-controls">
              <input
                type="file"
                id="avatar-upload"
                accept="image/*"
                onChange={handleAvatarUpload}
                style={{ display: "none" }}
              />
              <label htmlFor="avatar-upload" className="avatar-upload-btn">
                {isUploadingAvatar ? "Uploading..." : "Upload Avatar"}
              </label>

              {profileData.avatar && (
                <button
                  type="button"
                  className="avatar-remove-btn"
                  onClick={() => {
                    setProfileData((prev) => ({ ...prev, avatar: "" }));
                    setHasChanges(true);
                  }}
                >
                  Remove Avatar
                </button>
              )}
            </div>
          </div>
        </div>

        {/* ===== Action Buttons (Change Password + Save Changes) ===== */}
        <div className="profile-actions">
          <button
            className="change-password-btn"
            onClick={() => setIsChangePasswordOpen(true)}
          >
            Change Password
          </button>

          <button
            className="save-btn"
            onClick={handleSave}
            disabled={
              !hasChanges ||
              isSaving ||
              !profileData.name.trim() ||
              !profileData.accountName.trim()
            }
          >
            {isSaving ? "Saving..." : "Save Changes"}
          </button>
        </div>
      </div>

      {/* ===== Change Password Modal ===== */}
      <ChangePasswordModal
        isOpen={isChangePasswordOpen}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </div>
  );
}
