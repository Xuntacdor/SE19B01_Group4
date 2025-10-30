import React, { useEffect, useState } from "react";
import AppLayout from "../../Components/Layout/AppLayout";
import AdminNavbar from "../../Components/Admin/AdminNavbar.jsx";
import { Users, Search, Edit, Shield, User } from "lucide-react";
import { getAllUsersAdmin, getSignInHistory } from "../../Services/UserApi.js";
import { grantRole } from "../../Services/AdminApi.js";
import "./UserManagement.css";

import { useNavigate } from "react-router-dom";

export default function UserManagement() {
  const navigate = useNavigate();
  const [users, setUsers] = useState([]);
  const [filteredUsers, setFilteredUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedUser, setSelectedUser] = useState(null);
  const [showRoleModal, setShowRoleModal] = useState(false);

  useEffect(() => {
    loadUsers();
  }, []);

  useEffect(() => {
    filterUsers();
  }, [users, searchTerm]);

  const loadUsers = async () => {
    try {
      const response = await getAllUsersAdmin();
      const userData = response.data || [];
      setUsers(userData);
      setFilteredUsers(userData);
      setLoading(false);
    } catch (error) {
      console.error("Error loading users:", error);
      setError("Failed to load users");
      setUsers([]);
      setFilteredUsers([]);
      setLoading(false);
    }
  };

  const filterUsers = () => {
    if (!searchTerm.trim()) {
      setFilteredUsers(users);
      return;
    }

    const filtered = users.filter(
      (user) =>
        user.username?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        user.email?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        user.firstname?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        user.lastname?.toLowerCase().includes(searchTerm.toLowerCase())
    );
    setFilteredUsers(filtered);
  };

  const handleRoleChange = async (userId, newRole) => {
    try {
      await grantRole(userId, newRole);
      setUsers(users.map((user) =>
        user.userId === userId ? { ...user, role: newRole } : user
      ));
      setShowRoleModal(false);
      setSelectedUser(null);
      alert(`User role updated to ${newRole}`);
    } catch (error) {
      console.error("Error updating role:", error);
      alert("Failed to update user role");
    }
  };

  const openRoleModal = (user) => {
    setSelectedUser(user);
    setShowRoleModal(true);
  };

  const closeRoleModal = () => {
    setShowRoleModal(false);
    setSelectedUser(null);
  };

  const getRoleBadgeClass = (role) => {
    switch (role?.toLowerCase()) {
      case "admin":
        return "role-badge admin";
      case "moderator":
        return "role-badge moderator";
      case "user":
      default:
        return "role-badge user";
    }
  };

  const getRoleIcon = (role) => {
    switch (role?.toLowerCase()) {
      case "admin":
        return <Shield size={16} />;
      case "moderator":
        return <Edit size={16} />;
      case "user":
      default:
        return <User size={16} />;
    }
  };

  if (loading) {
    return (
      <AppLayout title="User Management" sidebar={<AdminNavbar />}>
        <div className="user-management-loading">
          <div className="loading-spinner"></div>
          <p>Loading users...</p>
        </div>
      </AppLayout>
    );
  }

  return (
    <AppLayout title="User Management" sidebar={<AdminNavbar />}>
      <div className="user-management">
        <div className="user-management-header">
          <div className="header-content">
            <Users size={28} style={{ marginLeft: "8px" }} />
            <h1>User Management</h1>
            <span className="user-count">({(filteredUsers || []).length} users)</span>
          </div>
        </div>

        {/* Search Bar */}
        <div className="search-section">
          <div className="search-container">
            <Search size={20} />
            <input
              type="text"
              placeholder="Search by username, email, or name..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="search-input"
            />
          </div>
        </div>

        {/* Users Table */}
        <div className="users-table-container">
          <table className="users-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>User</th>
                <th>Email</th>
                <th>Role</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map((user) => (
                <tr key={user.userId}>
                  <td>#{user.userId}</td>
                  <td>
                    <div className="user-info">
                      {user.avatar ? (
                        <img
                          src={user.avatar}
                          alt={user.username}
                          className="user-avatar"
                        />
                      ) : (
                        <div className="user-avatar-placeholder">
                          {(user.firstname?.[0] || user.username?.[0] || 'U').toUpperCase()}
                        </div>
                      )}
                      <div className="user-details">
                        <div className="username">{user.username}</div>
                        <div className="full-name">
                          {user.firstname} {user.lastname}
                        </div>
                      </div>
                    </div>
                  </td>
                  <td>{user.email}</td>
                  <td>
                    <span className={getRoleBadgeClass(user.role)}>
                      {getRoleIcon(user.role)}
                      {user.role || 'user'}
                    </span>
                  </td>
                  <td>
                    <button
                      className="action-btn change-role"
                      onClick={() => openRoleModal(user)}
                      disabled={user.role === "admin"}
                    >
                      <Edit size={14} />
                      Change Role
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {filteredUsers.length === 0 && (
            <div className="no-users">
              <Users size={48} />
              <p>No users found matching your search.</p>
            </div>
          )}
        </div>

        {/* Role Change Modal */}
        {showRoleModal && selectedUser && (
          <div className="modal-overlay">
            <div className="modal-content">
              <div className="modal-header">
                <h3>Change User Role</h3>
                <button className="close-btn" onClick={closeRoleModal}>
                  ×
                </button>
              </div>
              <div className="modal-body">
                <div className="user-info-modal">
                  <div className="modal-user-details">
                    <h4>{selectedUser.username}</h4>
                    <p>{selectedUser.firstname} {selectedUser.lastname}</p>
                    <p>{selectedUser.email}</p>
                  </div>
                  <span className={getRoleBadgeClass(selectedUser.role)}>
                    {getRoleIcon(selectedUser.role)}
                    Current: {selectedUser.role}
                  </span>
                </div>
                <div className="role-options">
                  <h4>Select New Role</h4>
                  <div className="role-buttons">
                    <button
                      className={`role-btn user ${
                        selectedUser.role === "user" ? "current" : ""
                      }`}
                      onClick={() => handleRoleChange(selectedUser.userId, "user")}
                    >
                      <User size={16} />
                      User
                    </button>
                    <button
                      className={`role-btn moderator ${
                        selectedUser.role === "moderator" ? "current" : ""
                      }`}
                      onClick={() => handleRoleChange(selectedUser.userId, "moderator")}
                    >
                      <Edit size={16} />
                      Moderator
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
