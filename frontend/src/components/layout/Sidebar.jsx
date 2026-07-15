import { NavLink } from "react-router-dom";
import { LayoutDashboard, UploadCloud, FileSearch, Users, LogOut, Workflow } from "lucide-react";
import { useAuth } from "../../context/useAuth";

const NAV_ITEMS = [
  { to: "/", label: "Tableau de bord", icon: LayoutDashboard, end: true },
  { to: "/importer", label: "Importer un flux", icon: UploadCloud },
  { to: "/documentations", label: "Documentations", icon: FileSearch },
];

export default function Sidebar() {
  const { user, isAdmin, logout } = useAuth();

  return (
    <aside
      style={{
        borderRight: "1px solid var(--color-border)",
        background: "var(--color-surface)",
        display: "flex",
        flexDirection: "column",
        padding: "var(--space-5) var(--space-4)",
        gap: "var(--space-6)",
      }}
    >
      <div className="row" style={{ gap: 10, padding: "0 var(--space-2)" }}>
        <div
          style={{
            width: 34,
            height: 34,
            borderRadius: 10,
            background: "var(--color-primary)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            color: "#fff",
            flexShrink: 0,
          }}
        >
          <Workflow size={18} />
        </div>
        <div style={{ lineHeight: 1.2 }}>
          <div style={{ fontFamily: "var(--font-display)", fontWeight: 700, fontSize: 14.5 }}>
            PA&nbsp;Doc&nbsp;Generator
          </div>
          <div style={{ fontSize: 11, color: "var(--color-muted)" }}>Documentation IA</div>
        </div>
      </div>

      <nav className="stack" style={{ gap: 2 }}>
        {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            style={({ isActive }) => ({
              display: "flex",
              alignItems: "center",
              gap: 10,
              padding: "9px 12px",
              borderRadius: "var(--radius-sm)",
              fontSize: 13.5,
              fontWeight: 600,
              textDecoration: "none",
              color: isActive ? "var(--color-primary)" : "var(--color-ink-soft)",
              background: isActive ? "var(--color-primary-soft)" : "transparent",
            })}
          >
            <Icon size={17} />
            {label}
          </NavLink>
        ))}

        {isAdmin && (
          <NavLink
            to="/administration"
            style={({ isActive }) => ({
              display: "flex",
              alignItems: "center",
              gap: 10,
              padding: "9px 12px",
              borderRadius: "var(--radius-sm)",
              fontSize: 13.5,
              fontWeight: 600,
              textDecoration: "none",
              color: isActive ? "var(--color-primary)" : "var(--color-ink-soft)",
              background: isActive ? "var(--color-primary-soft)" : "transparent",
            })}
          >
            <Users size={17} />
            Administration
          </NavLink>
        )}
      </nav>

      <div style={{ marginTop: "auto" }} className="stack">
        <div
          className="row"
          style={{
            gap: 10,
            padding: "var(--space-3)",
            borderRadius: "var(--radius-md)",
            background: "var(--color-surface-alt)",
            border: "1px solid var(--color-border)",
          }}
        >
          <div
            style={{
              width: 30,
              height: 30,
              borderRadius: "50%",
              background: "var(--color-primary-soft)",
              color: "var(--color-primary)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontWeight: 700,
              fontSize: 12,
              flexShrink: 0,
            }}
          >
            {(user?.fullName || user?.email || "?").slice(0, 1).toUpperCase()}
          </div>
          <div style={{ minWidth: 0, flex: 1 }}>
            <div style={{ fontSize: 12.5, fontWeight: 600, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
              {user?.fullName || user?.email}
            </div>
            <div style={{ fontSize: 11, color: "var(--color-muted)" }}>{user?.role}</div>
          </div>
          <button
            onClick={logout}
            title="Se déconnecter"
            style={{
              border: "none",
              background: "transparent",
              color: "var(--color-muted)",
              padding: 4,
              borderRadius: 6,
            }}
          >
            <LogOut size={16} />
          </button>
        </div>
      </div>
    </aside>
  );
}
