import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "./auth";

export function RequireAuth() {
  const { user, ready } = useAuth();
  if (!ready) return <div className="content">Loading CoreHR…</div>;
  if (!user) return <Navigate to="/login" replace />;
  return <Outlet />;
}
