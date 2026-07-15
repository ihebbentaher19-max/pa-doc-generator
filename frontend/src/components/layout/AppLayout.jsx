import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../../context/useAuth";
import Sidebar from "./Sidebar";

export function ProtectedRoute({ adminOnly = false }) {
  const { isAuthenticated, isAdmin } = useAuth();

  if (!isAuthenticated) return <Navigate to="/connexion" replace />;
  if (adminOnly && !isAdmin) return <Navigate to="/" replace />;

  return (
    <div className="app-shell">
      <Sidebar />
      <div className="main-column">
        <Outlet />
      </div>
    </div>
  );
}
