import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../../src/contexts/AuthContext'; // điều chỉnh path nếu cần

const ProtectedRoute = ({ children }) => {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return children;
};

export default ProtectedRoute;
