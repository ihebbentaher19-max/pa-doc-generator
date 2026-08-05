import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Search, FileText } from "lucide-react";
import PageHeader from "../components/ui/PageHeader";
import StatusBadge from "../components/ui/StatusBadge";
import EmptyState from "../components/ui/EmptyState";
import Spinner from "../components/ui/Spinner";
import { searchDocumentation } from "../services/documentationService";
import { inputStyle } from "../styles/formStyles";

const STATUS_FILTERS = [
  { value: "", label: "Tous les statuts" },
  { value: "Brouillon", label: "Brouillon" },
  { value: "Valide", label: "Validé" },
  { value: "Archive", label: "Archivé" },
];

export default function DocumentationListPage() {
  const [keyword, setKeyword] = useState("");
  const [status, setStatus] = useState("");
  const [results, setResults] = useState({ items: [], totalCount: 0 });
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const timeout = setTimeout(() => {
      setIsLoading(true);
      searchDocumentation({ keyword, status })
        .then(setResults)
        .finally(() => setIsLoading(false));
    }, 250); // léger debounce pour éviter une requête par frappe

    return () => clearTimeout(timeout);
  }, [keyword, status]);

  return (
    <div className="page-content">
      <PageHeader
        eyebrow="Module de recherche et consultation"
        title="Documentations"
        description="Recherchez une documentation par mot-clé, nom de flux ou statut."
      />

      <div className="row" style={{ gap: 10, marginBottom: "var(--space-5)" }}>
        <div className="row" style={{ flex: 1, position: "relative" }}>
          <Search size={16} style={{ position: "absolute", left: 12, color: "var(--color-muted)" }} />
          <input
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            placeholder="Rechercher par titre ou nom de flux…"
            style={{ ...inputStyle, width: "100%", paddingLeft: 34 }}
          />
        </div>
        <select value={status} onChange={(e) => setStatus(e.target.value)} style={{ ...inputStyle, minWidth: 160 }}>
          {STATUS_FILTERS.map((f) => (
            <option key={f.value} value={f.value}>{f.label}</option>
          ))}
        </select>
      </div>

      <div className="card" style={{ padding: "var(--space-5)" }}>
        {isLoading ? (
          <div className="row" style={{ gap: 8, color: "var(--color-muted)" }}>
            <Spinner /> Recherche en cours…
          </div>
        ) : results.items.length === 0 ? (
          <EmptyState
            icon={FileText}
            title="Aucun résultat"
            description="Ajustez votre recherche ou importez un nouveau flux pour générer sa documentation."
          />
        ) : (
          <div className="stack" style={{ gap: 0 }}>
            {results.items.map((doc) => (
              <Link
                key={doc.id}
                to={`/documentations/${doc.id}`}
                className="row"
                style={{
                  justifyContent: "space-between",
                  padding: "13px 4px",
                  borderBottom: "1px solid var(--color-border)",
                  textDecoration: "none",
                  color: "inherit",
                }}
              >
                <div className="stack" style={{ gap: 2, minWidth: 0 }}>
                  <span style={{ fontWeight: 600, fontSize: 13.5 }}>{doc.title}</span>
                  <span style={{ fontSize: 12, color: "var(--color-muted)" }}>
                    {doc.flowName} · v{doc.currentVersionNumber} · {doc.createdByUserName}
                  </span>
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
    </div>
  );
}
