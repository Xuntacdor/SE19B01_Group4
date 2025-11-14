import { useEffect } from "react";
import { Navigate, Outlet } from "react-router-dom";
import useAuth from "./UseAuth";

export default function Authorization({ allow, redirect = "/" }) {
  const { user, loading } = useAuth();

  if (loading) {
    return; // Or a proper loading component
  }

  if (!user) {
    return <Navigate to="/" replace />;
  }

  if (allow && !allow.includes(user.role)) {
    return <Navigate to={redirect} replace />;
  }

  return <Outlet />;
}
