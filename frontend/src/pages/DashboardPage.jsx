import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { FileText, Workflow, PenLine, CheckCircle2, Archive, RefreshCw } from "lucide-react";
import { getDashboardStats } from "../services/dashboardService";
import PageHeader from "../components/ui/PageHeader";
import StatusBadge from "../components/ui/StatusBadge";
import EmptyState from "../components/ui/EmptyState";
import Spinner from "../components/ui/Spinner";
import Button from "../components/ui/Button";
import { useAuth } from "../context/useAuth";

const STAT_CARDS = [
  { key: "totalDocumentations", label: "Documentations générées", icon: FileText },
  { key: "totalFlowsImported", label: "Flux importés", icon: Workflow },
  { key: "draftCount", label: "Brouillons", icon: PenLine },
  { key: "validatedCount", label: "Validées", icon: CheckCircle2 },
  { key: "archivedCount", label: "Archivées", icon: Archive },
];

export default function DashboardPage() {
  const { isAdmin } = useAuth();
  const [stats, setStats] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState(null);

  const loadStats = useCallback((showSpinner) => {
    if (showSpinner) setIsLoading(true);
    else setIsRefreshing(true);
    setError(null);
    return getDashboardStats()
      .then(setStats)
      .catch(() => setError("Impossible de charger les statistiques pour le moment."))
      .finally(() => {
        setIsLoading(false);
        setIsRefreshing(false);
      });
  }, []);

  useEffect(() => {
    loadStats(true);
  }, [loadStats]);

  return (
    <div className="page-content">
      <PageHeader
        eyebrow="Vue d'ensemble"
        title="Tableau de bord"
        description={
          isAdmin
            ? "Activité globale de la plateforme : flux importés, documentations générées et leur statut."
            : "Votre activité : vos flux importés, vos documentations générées et leur statut."
        }
        actions={
          <>
            <Button variant="secondary" onClick={() => loadStats(false)} disabled={isRefreshing}>
              <RefreshCw size={15} /> {isRefreshing ? "Actualisation…" : "Actualiser"}
            </Button>
            <Link to="/importer">
              <Button>Importer un flux</Button>
            </Link>
          </>
        }
      />

      {isLoading && (
        <div className="row" style={{ gap: 8, color: "var(--color-muted)" }}>
          <Spinner /> Chargement…
        </div>
      )}

      {error && !isLoading && <p style={{ color: "var(--color-danger)" }}>{error}</p>}

      {stats && !isLoading && (
        <>
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fit, minmax(160px, 1fr))",
              gap: "var(--space-4)",
              marginBottom: "var(--space-6)",
            }}
          >
            {STAT_CARDS.map(({ key, label, icon: Icon }) => (
              <div key={key} className="card" style={{ padding: "var(--space-4)" }}>
                <div className="row" style={{ justifyContent: "space-between", marginBottom: 10 }}>
                  <span style={{ fontSize: 12, color: "var(--color-muted)", fontWeight: 600 }}>{label}</span>
                  <Icon size={16} color="var(--color-primary)" />
                </div>
                <div style={{ fontFamily: "var(--font-display)", fontSize: 28, fontWeight: 700 }}>
                  {stats[key]}
                </div>
              </div>
            ))}
          </div>

          <div className="card" style={{ padding: "var(--space-5)" }}>
            <div className="row" style={{ justifyContent: "space-between", marginBottom: "var(--space-4)" }}>
              <h2>Dernières activités</h2>
              <Link to="/documentations" style={{ fontSize: 13, fontWeight: 600, color: "var(--color-primary)" }}>
                Tout voir →
              </Link>
            </div>

            {stats.recentDocumentations.length === 0 ? (
              <EmptyState
                icon={FileText}
                title="Aucune documentation pour le moment"
                description="Importez un premier flux Power Automate pour générer sa documentation."
                action={
                  <Link to="/importer">
                    <Button variant="secondary">Importer un flux</Button>
                  </Link>
                }
              />
            ) : (
              <div className="stack" style={{ gap: 0 }}>
                {stats.recentDocumentations.map((doc) => (
                  <Link
                    key={doc.id}
                    to={`/documentations/${doc.id}`}
                    className="row"
                    style={{
                      justifyContent: "space-between",
                      padding: "12px 4px",
                      borderBottom: "1px solid var(--color-border)",
                      textDecoration: "none",
                      color: "inherit",
                    }}
                  >
                    <div className="stack" style={{ gap: 2, minWidth: 0 }}>
                      <span style={{ fontWeight: 600, fontSize: 13.5, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
                        {doc.title}
                      </span>
                      <span style={{ fontSize: 12, color: "var(--color-muted)" }}>{doc.flowName} · {doc.createdByUserName}</span>
                    </div>
                    <div className="row" style={{ gap: 14, flexShrink: 0 }}>
                      <span style={{ fontSize: 12, color: "var(--color-muted)" }}>
                        {new Date(doc.updatedAtUtc).toLocaleDateString("fr-FR")}
                      </span>
                      <StatusBadge status={doc.status} />
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
