import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Download, History, Star, Plus, Trash2, Save, RefreshCw } from "lucide-react";
import PageHeader from "../components/ui/PageHeader";
import StatusBadge from "../components/ui/StatusBadge";
import Button from "../components/ui/Button";
import Callout from "../components/ui/Callout";
import Spinner from "../components/ui/Spinner";
import { useAuth } from "../context/useAuth";
import {
  getDocumentation,
  updateDocumentation,
  changeDocumentationStatus,
  getVersionHistory,
  downloadExport,
  regenerateDocumentation,
  deleteDocumentation,
} from "../services/documentationService";
import { getApiErrorMessage } from "../services/api";
import { inputStyle } from "../styles/formStyles";

const STATUS_OPTIONS = ["Brouillon", "Valide", "Archive"];

export default function DocumentationDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { isAdmin } = useAuth();

  const [doc, setDoc] = useState(null);
  const [content, setContent] = useState(null);
  const [title, setTitle] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);

  const [isSaving, setIsSaving] = useState(false);
  const [saveMessage, setSaveMessage] = useState(null);
  const [isExporting, setIsExporting] = useState(null);
  const [isRegenerating, setIsRegenerating] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const [showHistory, setShowHistory] = useState(false);
  const [versions, setVersions] = useState([]);

  function loadDocument() {
    setIsLoading(true);
    getDocumentation(id)
      .then((data) => {
        setDoc(data);
        setContent(data.content);
        setTitle(data.title);
      })
      .catch((err) => setError(getApiErrorMessage(err, "Documentation introuvable.")))
      .finally(() => setIsLoading(false));
  }

  useEffect(loadDocument, [id]);

  async function handleSave() {
    setIsSaving(true);
    setSaveMessage(null);
    try {
      const updated = await updateDocumentation(id, title, content, "Modification manuelle depuis l'interface.");
      setDoc(updated);
      setContent(updated.content);
      setSaveMessage({ tone: "success", text: `Enregistré — version v${updated.currentVersionNumber}.` });
    } catch (err) {
      setSaveMessage({ tone: "error", text: getApiErrorMessage(err, "L'enregistrement a échoué.") });
    } finally {
      setIsSaving(false);
    }
  }

  async function handleStatusChange(newStatus) {
    try {
      const updated = await changeDocumentationStatus(id, newStatus);
      setDoc(updated);
    } catch (err) {
      setSaveMessage({ tone: "error", text: getApiErrorMessage(err, "Le changement de statut a échoué.") });
    }
  }

  async function handleExport(format) {
    setIsExporting(format);
    try {
      await downloadExport(id, format, `${title || "documentation"}.${format === "word" ? "docx" : "pdf"}`);
    } catch (err) {
      setSaveMessage({ tone: "error", text: getApiErrorMessage(err, "L'export a échoué.") });
    } finally {
      setIsExporting(null);
    }
  }

  /** Relance la génération IA sur le flux d'origine (backlog : "Permettre la régénération"). */
  async function handleRegenerate() {
    if (!window.confirm("Relancer la génération IA ? Le contenu actuel sera remplacé par une nouvelle version.")) {
      return;
    }
    setIsRegenerating(true);
    setSaveMessage(null);
    try {
      const updated = await regenerateDocumentation(id);
      setDoc(updated);
      setContent(updated.content);
      setTitle(updated.title);
      setSaveMessage({ tone: "success", text: `Régénéré par l'IA — nouvelle version v${updated.currentVersionNumber}.` });
    } catch (err) {
      setSaveMessage({ tone: "error", text: getApiErrorMessage(err, "La régénération a échoué.") });
    } finally {
      setIsRegenerating(false);
    }
  }

  /** Suppression définitive - réservée aux administrateurs côté backend ET masquée côté
   * interface pour les autres profils (backlog : "Masquer les actions interdites côté interface"). */
  async function handleDelete() {
    if (!window.confirm("Supprimer définitivement cette documentation ? Cette action est irréversible.")) {
      return;
    }
    setIsDeleting(true);
    try {
      await deleteDocumentation(id);
      navigate("/documentations");
    } catch (err) {
      setSaveMessage({ tone: "error", text: getApiErrorMessage(err, "La suppression a échoué.") });
      setIsDeleting(false);
    }
  }

  async function toggleHistory() {
    if (!showHistory) {
      const data = await getVersionHistory(id);
      setVersions(data);
    }
    setShowHistory((v) => !v);
  }

  function updateStep(index, field, value) {
    setContent((c) => ({
      ...c,
      steps: c.steps.map((s, i) => (i === index ? { ...s, [field]: value } : s)),
    }));
  }

  function removeStep(index) {
    setContent((c) => ({ ...c, steps: c.steps.filter((_, i) => i !== index) }));
  }

  function addStep() {
    setContent((c) => ({
      ...c,
      steps: [...c.steps, { stepName: "Nouvelle étape", description: "", isImportant: false }],
    }));
  }

  if (isLoading) {
    return (
      <div className="page-content row" style={{ gap: 8, color: "var(--color-muted)" }}>
        <Spinner /> Chargement de la documentation…
      </div>
    );
  }

  if (error) {
    return (
      <div className="page-content">
        <Callout tone="error">{error}</Callout>
      </div>
    );
  }

  return (
    <div className="page-content">
      <PageHeader
        eyebrow={doc.flowName}
        title={
          <input
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            style={{ ...inputStyle, fontFamily: "var(--font-display)", fontWeight: 700, fontSize: 24, width: "100%", border: "none", padding: "2px 0" }}
          />
        }
        actions={
          <>
            <select
              value={doc.status}
              onChange={(e) => handleStatusChange(e.target.value)}
              style={{ ...inputStyle, fontWeight: 600 }}
            >
              {STATUS_OPTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
            <Button variant="secondary" onClick={toggleHistory}>
              <History size={15} /> Historique
            </Button>
            <Button variant="secondary" onClick={handleRegenerate} disabled={isRegenerating}>
              <RefreshCw size={15} /> {isRegenerating ? "Régénération…" : "Régénérer via IA"}
            </Button>
            <Button variant="secondary" onClick={() => handleExport("pdf")} disabled={isExporting === "pdf"}>
              <Download size={15} /> {isExporting === "pdf" ? "Export…" : "PDF"}
            </Button>
            <Button variant="secondary" onClick={() => handleExport("word")} disabled={isExporting === "word"}>
              <Download size={15} /> {isExporting === "word" ? "Export…" : "Word"}
            </Button>
            <Button onClick={handleSave} disabled={isSaving}>
              <Save size={15} /> {isSaving ? "Enregistrement…" : "Enregistrer"}
            </Button>
            {isAdmin && (
              <Button variant="danger" onClick={handleDelete} disabled={isDeleting}>
                <Trash2 size={15} /> {isDeleting ? "Suppression…" : "Supprimer"}
              </Button>
            )}
          </>
        }
      />

      <div className="row" style={{ gap: 10, marginBottom: "var(--space-5)" }}>
        <StatusBadge status={doc.status} />
        <span style={{ fontSize: 12.5, color: "var(--color-muted)" }}>
          Version v{doc.currentVersionNumber} · mis à jour le {new Date(doc.updatedAtUtc).toLocaleString("fr-FR")}
        </span>
      </div>

      {saveMessage && (
        <div style={{ marginBottom: "var(--space-4)" }}>
          <Callout tone={saveMessage.tone}>{saveMessage.text}</Callout>
        </div>
      )}

      {showHistory && (
        <div className="card" style={{ padding: "var(--space-4)", marginBottom: "var(--space-5)" }}>
          <h3 style={{ marginBottom: 10 }}>Historique des versions</h3>
          <div className="stack" style={{ gap: 8 }}>
            {versions.map((v) => (
              <div key={v.versionNumber} className="row" style={{ justifyContent: "space-between", fontSize: 13 }}>
                <span>
                  <strong>v{v.versionNumber}</strong> — {v.isManuallyEdited ? "modification manuelle" : "génération IA"} par {v.editedByFullName}
                  {v.changeNote ? ` — ${v.changeNote}` : ""}
                </span>
                <span style={{ color: "var(--color-muted)" }}>{new Date(v.createdAtUtc).toLocaleString("fr-FR")}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Résumé fonctionnel */}
      <section className="card" style={{ padding: "var(--space-5)", marginBottom: "var(--space-5)" }}>
        <h2 style={{ marginBottom: 12 }}>Résumé fonctionnel</h2>
        <textarea
          value={content.functionalSummary}
          onChange={(e) => setContent((c) => ({ ...c, functionalSummary: e.target.value }))}
          rows={4}
          style={{ ...inputStyle, width: "100%", resize: "vertical", fontFamily: "var(--font-body)" }}
        />
      </section>

      {/* Étapes */}
      <section className="card" style={{ padding: "var(--space-5)", marginBottom: "var(--space-5)" }}>
        <div className="row" style={{ justifyContent: "space-between", marginBottom: 12 }}>
          <h2>Étapes du flux</h2>
          <Button variant="ghost" onClick={addStep}><Plus size={15} /> Ajouter une étape</Button>
        </div>
        <div className="stack" style={{ gap: 10 }}>
          {content.steps.map((step, index) => (
            <div key={index} className="card" style={{ padding: "var(--space-4)", background: "var(--color-surface-alt)" }}>
              <div className="row" style={{ gap: 8, marginBottom: 8 }}>
                <input
                  value={step.stepName}
                  onChange={(e) => updateStep(index, "stepName", e.target.value)}
                  style={{ ...inputStyle, flex: 1, fontWeight: 600 }}
                />
                <button
                  onClick={() => updateStep(index, "isImportant", !step.isImportant)}
                  title="Marquer comme étape importante"
                  style={{
                    border: "1px solid var(--color-border)",
                    background: step.isImportant ? "var(--color-accent-soft)" : "var(--color-surface)",
                    borderRadius: "var(--radius-sm)",
                    padding: "8px 10px",
                  }}
                >
                  <Star size={15} fill={step.isImportant ? "var(--color-accent)" : "none"} color="var(--color-accent)" />
                </button>
                <button onClick={() => removeStep(index)} style={{ border: "none", background: "transparent", color: "var(--color-danger)", padding: 8 }}>
                  <Trash2 size={15} />
                </button>
              </div>
              <textarea
                value={step.description}
                onChange={(e) => updateStep(index, "description", e.target.value)}
                rows={2}
                style={{ ...inputStyle, width: "100%", resize: "vertical" }}
              />
            </div>
          ))}
        </div>
      </section>

      {/* Dépendances */}
      {content.dependencies.length > 0 && (
        <section className="card" style={{ padding: "var(--space-5)", marginBottom: "var(--space-5)" }}>
          <h2 style={{ marginBottom: 12 }}>Dépendances entre actions, conditions et variables</h2>
          <div className="stack" style={{ gap: 8 }}>
            {content.dependencies.map((dep, index) => (
              <div key={index} style={{ fontSize: 13.5, padding: "8px 0", borderBottom: "1px solid var(--color-border)" }}>
                <strong>{dep.from} → {dep.to}</strong> : {dep.explanationText}
              </div>
            ))}
          </div>
        </section>
      )}

      {/* Étapes importantes */}
      {content.importantSteps.length > 0 && (
        <section className="card" style={{ padding: "var(--space-5)" }}>
          <h2 style={{ marginBottom: 12 }}>Étapes importantes à retenir</h2>
          <div className="stack" style={{ gap: 8 }}>
            {content.importantSteps.map((step, index) => (
              <div key={index} className="row" style={{ gap: 8, fontSize: 13.5 }}>
                <Star size={14} fill="var(--color-accent)" color="var(--color-accent)" />
                {step}
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
