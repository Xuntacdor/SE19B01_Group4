import React, { useEffect } from "react";
import { useNavigate } from "react-router-dom";

export default function AdminDashboard() {
  const navigate = useNavigate();
  
  useEffect(() => {
    // Redirect to User Management page
    navigate("/admin/users", { replace: true });
  }, [navigate]);

  return null;
}
