import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useState } from "react";
import { useAuth } from "../auth";
import { initials } from "../lib/format";

const links = [
  { to: "/", label: "Dashboard", icon: "speedometer2", end: true },
  { to: "/notifications", label: "Notifications", icon: "bell" },
  { to: "/people", label: "People Management", icon: "people" },
  { to: "/leave", label: "Leave Management", icon: "calendar2-week" },
  { to: "/recruitment", label: "Recruitment", icon: "briefcase" },
  { to: "/onboarding", label: "Onboarding", icon: "person-check" },
  { to: "/templates", label: "Templates", icon: "file-earmark-text" },
  { to: "/training", label: "Training", icon: "mortarboard" },
  { to: "/benefits", label: "Benefits", icon: "heart-pulse" },
  { to: "/performance", label: "Performance", icon: "graph-up-arrow" },
  { to: "/attendance", label: "Attendance", icon: "clock" }
];

const titles: Record<string, string> = {
  "/": "Admin Dashboard",
  "/notifications": "Notifications",
  "/people": "People Management",
  "/leave": "Leave Management",
  "/recruitment": "Recruitment",
  "/onboarding": "Onboarding",
  "/templates": "Templates",
  "/training": "Training",
  "/benefits": "Benefits",
  "/performance": "Performance",
  "/attendance": "Attendance",
  "/reports": "Reports",
  "/organization": "Organization",
  "/administration": "Administration"
};

export function Layout() {
  const { user, logout, isAdmin } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [reportsOpen, setReportsOpen] = useState(location.pathname.startsWith("/reports"));
  const [orgOpen, setOrgOpen] = useState(location.pathname.startsWith("/organization"));
  const title = location.pathname === "/" && !isAdmin ? "Dashboard" : titles[location.pathname] ?? "CoreHR";

  return (
    <div className="app-shell">
      {open && <button className="backdrop" aria-label="Close menu" onClick={() => setOpen(false)} />}
      <aside className={open ? "sidebar open" : "sidebar"}>
        <div className="brand"><span>Core</span><span>HR</span></div>
        <div className="nav-label">NAVIGATION</div>
        {links.map((link) => (
          <NavLink key={link.to} to={link.to} end={link.end} className={({ isActive }) => isActive ? "side-link active" : "side-link"} onClick={() => setOpen(false)}>
            <i className={`bi bi-${link.icon}`} /> {link.label}
          </NavLink>
        ))}
        <button className="side-group" onClick={() => setReportsOpen((value) => !value)}>
          <i className="bi bi-bar-chart" /> Reports <i className={`bi bi-chevron-${reportsOpen ? "up" : "down"} chevron`} />
        </button>
        {reportsOpen && (
          <div className="subnav">
            <NavLink to="/reports?tab=headcount" onClick={() => setOpen(false)}>Headcount</NavLink>
            <NavLink to="/reports?tab=attendance" onClick={() => setOpen(false)}>Attendance</NavLink>
            <NavLink to="/reports?tab=leave" onClick={() => setOpen(false)}>Leave</NavLink>
          </div>
        )}
        <button className="side-group" onClick={() => setOrgOpen((value) => !value)}>
          <i className="bi bi-diagram-3" /> Organization <i className={`bi bi-chevron-${orgOpen ? "up" : "down"} chevron`} />
        </button>
        {orgOpen && (
          <div className="subnav">
            <NavLink to="/organization" onClick={() => setOpen(false)}>Departments</NavLink>
            <NavLink to="/organization#positions" onClick={() => setOpen(false)}>Positions</NavLink>
          </div>
        )}
        {isAdmin && (
          <NavLink to="/administration" className={({ isActive }) => isActive ? "side-link active" : "side-link"} onClick={() => setOpen(false)}>
            <i className="bi bi-gear" /> Administration
          </NavLink>
        )}
      </aside>
      <div className="workspace">
        <header className="topbar">
          <div className="d-flex align-items-center gap-2">
            <button className="menu-btn" onClick={() => setOpen(true)} aria-label="Open menu"><i className="bi bi-list" /></button>
            <h1>{title}</h1>
          </div>
          <div className="who">
            <div className="text-end">
              <strong>{user?.email}</strong>
              <small>{user?.roles[0] ?? "User"}</small>
            </div>
            <div className="avatar" title={user?.fullName}>{initials(user?.fullName ?? user?.email ?? "?")}</div>
            <button className="btn btn-sm btn-outline-secondary" onClick={() => { logout(); navigate("/login"); }}>Sign out</button>
          </div>
        </header>
        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
