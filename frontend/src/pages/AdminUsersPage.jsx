import { useEffect, useState } from "react";
import { ShieldCheck, ShieldOff } from "lucide-react";
import PageHeader from "../components/ui/PageHeader";
import Spinner from "../components/ui/Spinner";
import Callout from "../components/ui/Callout";
import { listUsers, changeUserRole, setUserActive } from "../services/usersService";
import { getApiErrorMessage } from "../services/api";
import { useAuth } from "../context/useAuth";

export default function AdminUsersPage() {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);

  function load() {
    setIsLoading(true);
    listUsers()
      .then(setUsers)
      .catch((err) => setError(getApiErrorMessage(err)))
      .finally(() => setIsLoading(false));
  }

  useEffect(load, []);

  async function toggleRole(user) {
    setError(null);
    const newRole = user.role === "Administrateur" ? "Utilisateur" : "Administrateur";
    try {
      await changeUserRole(user.id, newRole);
      load();
    } catch (err) {
      setError(getApiErrorMessage(err, "Le changement de rôle a échoué."));
    }
  }

  async function toggleActive(user) {
    setError(null);
    try {
      await setUserActive(user.id, !user.isActive);
      load();
    } catch (err) {
      setError(getApiErrorMessage(err, "Le changement de statut a échoué."));
    }
  }

  return (
    <div className="page-content">
      <PageHeader
        eyebrow="Module de gestion des rôles"
        title="Administration des comptes"
        description="Attribution des rôles (administrateur / utilisateur) et activation des comptes de la plateforme."
      />

      {error && <Callout tone="error">{error}</Callout>}

      {isLoading ? (
        <div className="row" style={{ gap: 8, color: "var(--color-muted)" }}><Spinner /> Chargement…</div>
      ) : (
        <div className="card" style={{ padding: "var(--space-5)" }}>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13.5 }}>
            <thead>
              <tr style={{ textAlign: "left", color: "var(--color-muted)", fontSize: 12 }}>
                <th style={{ padding: "8px 4px" }}>Nom</th>
                <th style={{ padding: "8px 4px" }}>E-mail</th>
                <th style={{ padding: "8px 4px" }}>Rôle</th>
                <th style={{ padding: "8px 4px" }}>Statut</th>
                <th style={{ padding: "8px 4px" }}></th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => {
                const isSelf = u.id === currentUser?.id;
                return (
                  <tr key={u.id} style={{ borderTop: "1px solid var(--color-border)" }}>
                    <td style={{ padding: "10px 4px", fontWeight: 600 }}>
                      {u.fullName}
                      {isSelf && <span style={{ color: "var(--color-muted)", fontWeight: 400 }}> (vous)</span>}
                    </td>
                    <td style={{ padding: "10px 4px", color: "var(--color-muted)" }}>{u.email}</td>
                    <td style={{ padding: "10px 4px" }}>
                      <button
                        onClick={() => toggleRole(u)}
                        disabled={isSelf}
                        title={isSelf ? "Vous ne pouvez pas modifier votre propre rôle." : undefined}
                        className="row"
                        style={{
                          gap: 6,
                          border: "1px solid var(--color-border)",
                          background: "var(--color-surface)",
                          borderRadius: 6,
                          padding: "5px 10px",
                          fontSize: 12.5,
                          fontWeight: 600,
                          opacity: isSelf ? 0.5 : 1,
                          cursor: isSelf ? "not-allowed" : "pointer",
                        }}
                      >
                        {u.role === "Administrateur" ? <ShieldCheck size={14} color="var(--color-primary)" /> : <ShieldOff size={14} color="var(--color-muted)" />}
                        {u.role}
                      </button>
                    </td>
                    <td style={{ padding: "10px 4px" }}>
                      <span style={{ color: u.isActive ? "var(--color-success)" : "var(--color-danger)", fontWeight: 600 }}>
                        {u.isActive ? "Actif" : "Désactivé"}
                      </span>
                    </td>
                    <td style={{ padding: "10px 4px", textAlign: "right" }}>
                      <button
                        onClick={() => toggleActive(u)}
                        disabled={isSelf}
                        title={isSelf ? "Vous ne pouvez pas désactiver votre propre compte." : undefined}
                        style={{
                          border: "none",
                          background: "transparent",
                          color: isSelf ? "var(--color-muted)" : "var(--color-primary)",
                          fontWeight: 600,
                          fontSize: 12.5,
                          cursor: isSelf ? "not-allowed" : "pointer",
                        }}
                      >
                        {u.isActive ? "Désactiver" : "Réactiver"}
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}