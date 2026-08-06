import { NavLink } from "react-router-dom";
import {
  LayoutDashboard,
  UploadCloud,
  FileSearch,
  Users,
  LogOut,
  Workflow
} from "lucide-react";
import { useAuth } from "../../context/useAuth";

const NAV_ITEMS = [
  {
    to: "/",
    label: "Tableau de bord",
    icon: LayoutDashboard,
    end: true
  },
  {
    to: "/importer",
    label: "Importer un flux",
    icon: UploadCloud
  },
  {
    to: "/documentations",
    label: "Documentations",
    icon: FileSearch
  }
];

export default function Sidebar() {
  const { user, isAdmin, logout } = useAuth();

  const navStyle = (isActive) => ({
    cursor: "pointer",
    display: "flex",
    alignItems: "center",
    gap: 14,

    padding: "12px 16px",

    marginBottom: 6,

    borderRadius: "12px",

    textDecoration: "none",

    fontSize: 14,

    fontWeight: 600,

    transition: "all .2s ease",

    color: isActive
      ? "var(--color-primary)"
      : "var(--color-ink-soft)",

    background: isActive
      ? "var(--color-primary-soft)"
      : "transparent",

    borderLeft: isActive
      ? "4px solid var(--color-primary)"
      : "4px solid transparent"
  });

  return (
    <aside
      style={{
        background: "#ffffff",

        borderRight: "1px solid var(--color-border)",

        display: "flex",

        flexDirection: "column",

        padding: "28px 18px",

        gap: 34,

        boxShadow: "2px 0 12px rgba(0,0,0,.03)"
      }}
    >
      {/* Logo */}

      <div
        className="row"
        style={{
          gap: 14,
          paddingLeft: 8
        }}
      >
        <div
          style={{
            width: 46,

            height: 46,

            borderRadius: 14,

            background:
              "linear-gradient(135deg,#0078D4,#00BCF2)",

            display: "flex",

            alignItems: "center",

            justifyContent: "center",

            color: "#fff",

            boxShadow: "0 8px 20px rgba(0,120,212,.25)"
          }}
        >
          <Workflow size={22} />
        </div>

        <div>
          <div
            style={{
              fontWeight: 700,

              fontSize: 18,

              lineHeight: 1.2
            }}
          >
            Générateur de documentation IA pour Power Automate
          </div>
        </div>
      </div>

      {/* Navigation */}

      <nav
        style={{
          display: "flex",

          flexDirection: "column"
        }}
      >
        {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            style={({ isActive }) => navStyle(isActive)}
          >
            <Icon size={19} />

            <span>{label}</span>
          </NavLink>
        ))}

        {isAdmin && (
          <NavLink
            to="/administration"
            style={({ isActive }) => navStyle(isActive)}
          >
            <Users size={19} />

            Administration
          </NavLink>
        )}
      </nav>

      {/* Utilisateur */}

      <div style={{ marginTop: "auto" }}>
        <div
          style={{
            border: "1px solid var(--color-border)",

            borderRadius: 16,

            padding: 14,

            background: "#fff",

            display: "flex",

            alignItems: "center",

            gap: 12,

            boxShadow: "var(--shadow-sm)"
          }}
        >
          <div
            style={{
              width: 42,

              height: 42,

              borderRadius: "50%",

              background: "var(--color-primary-soft)",

              color: "var(--color-primary)",

              display: "flex",

              alignItems: "center",

              justifyContent: "center",

              fontWeight: 700,

              fontSize: 15
            }}
          >
            {(user?.fullName || user?.email || "?")
              .charAt(0)
              .toUpperCase()}
          </div>

          <div
            style={{
              flex: 1,

              overflow: "hidden"
            }}
          >
            <div
              style={{
                fontWeight: 600,

                fontSize: 14,

                whiteSpace: "nowrap",

                overflow: "hidden",

                textOverflow: "ellipsis"
              }}
            >
              {user?.fullName || user?.email}
            </div>

            <div
              style={{
                fontSize: 12,

                color: "var(--color-muted)"
              }}
            >
              {user?.role}
            </div>
          </div>

          <button
            onClick={logout}
            title="Déconnexion"
            style={{
              border: "none",

              background: "transparent",

              cursor: "pointer",

              color: "var(--color-muted)",

              padding: 6,

              borderRadius: 8
            }}
          >
            <LogOut size={18} />
          </button>
        </div>
      </div>
    </aside>
  );
}