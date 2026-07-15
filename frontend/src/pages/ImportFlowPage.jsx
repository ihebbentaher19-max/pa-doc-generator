import { useCallback, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { UploadCloud, FileJson, CheckCircle2, XCircle } from "lucide-react";
import PageHeader from "../components/ui/PageHeader";
import Button from "../components/ui/Button";
import Callout from "../components/ui/Callout";
import PipelineTrail from "../components/ui/PipelineTrail";
import { importFlow } from "../services/flowsService";
import { generateDocumentation } from "../services/documentationService";
import { getApiErrorMessage } from "../services/api";

// Étapes affichées pendant l'appel à /documentation/generate : le backend
// exécute ce pipeline en une seule requête, on anime la progression pour que
// l'utilisateur comprenne ce qui se passe (cf. section 6 du cahier des charges).
const GENERATION_STAGE_DELAYS_MS = [600, 1400, 900];

export default function ImportFlowPage() {
  const navigate = useNavigate();
  const fileInputRef = useRef(null);

  const [fileName, setFileName] = useState("");
  const [jsonContent, setJsonContent] = useState("");
  const [isDraggingOver, setIsDraggingOver] = useState(false);

  const [importResult, setImportResult] = useState(null);
  const [isImporting, setIsImporting] = useState(false);
  const [importError, setImportError] = useState(null);

  const [pipelineStep, setPipelineStep] = useState(null);
  const [isGenerating, setIsGenerating] = useState(false);
  const [generationError, setGenerationError] = useState(null);

  const readFile = useCallback((file) => {
    setFileName(file.name);
    setImportResult(null);
    setImportError(null);
    const reader = new FileReader();
    reader.onload = () => setJsonContent(String(reader.result || ""));
    reader.readAsText(file);
  }, []);

  function handleDrop(e) {
    e.preventDefault();
    setIsDraggingOver(false);
    const file = e.dataTransfer.files?.[0];
    if (file) readFile(file);
  }

  async function handleImport() {
    if (!jsonContent.trim()) return;
    setIsImporting(true);
    setImportError(null);
    setImportResult(null);
    try {
      const result = await importFlow(fileName || "flux.json", jsonContent);
      setImportResult(result);
    } catch (err) {
      setImportError(getApiErrorMessage(err, "L'import a échoué."));
    } finally {
      setIsImporting(false);
    }
  }

  async function handleGenerate() {
    if (!importResult) return;
    setIsGenerating(true);
    setGenerationError(null);
    setPipelineStep("lecture");

    const timers = [
      setTimeout(() => setPipelineStep("generation"), GENERATION_STAGE_DELAYS_MS[0]),
      setTimeout(() => setPipelineStep("mise-en-forme"), GENERATION_STAGE_DELAYS_MS[0] + GENERATION_STAGE_DELAYS_MS[1]),
    ];

    try {
      const documentation = await generateDocumentation(importResult.flowImportId);
      setPipelineStep("enregistrement");
      setTimeout(() => navigate(`/documentations/${documentation.id}`), 500);
    } catch (err) {
      setGenerationError(getApiErrorMessage(err, "La génération de documentation a échoué."));
      setPipelineStep(null);
    } finally {
      timers.forEach(clearTimeout);
      setIsGenerating(false);
    }
  }

  return (
    <div className="page-content">
      <PageHeader
        eyebrow="Module d'importation"
        title="Importer un flux Power Automate"
        description="Chargez un flux exporté au format JSON. La plateforme vérifie sa conformité avant de lancer la génération de documentation."
      />

      <div className="card" style={{ padding: "var(--space-5)", marginBottom: "var(--space-5)" }}>
        <div
          onDragOver={(e) => { e.preventDefault(); setIsDraggingOver(true); }}
          onDragLeave={() => setIsDraggingOver(false)}
          onDrop={handleDrop}
          onClick={() => fileInputRef.current?.click()}
          style={{
            border: `2px dashed ${isDraggingOver ? "var(--color-primary)" : "var(--color-border)"}`,
            borderRadius: "var(--radius-md)",
            background: isDraggingOver ? "var(--color-primary-soft)" : "var(--color-surface-alt)",
            padding: "var(--space-7) var(--space-5)",
            textAlign: "center",
            cursor: "pointer",
            transition: "background 120ms ease, border-color 120ms ease",
          }}
        >
          <input
            ref={fileInputRef}
            type="file"
            accept=".json,application/json"
            hidden
            onChange={(e) => e.target.files?.[0] && readFile(e.target.files[0])}
          />
          <UploadCloud size={26} color="var(--color-primary)" style={{ marginBottom: 10 }} />
          <p style={{ fontWeight: 600, fontSize: 14 }}>
            {fileName ? fileName : "Glissez-déposez un fichier .json, ou cliquez pour parcourir"}
          </p>
          <p style={{ fontSize: 12.5, color: "var(--color-muted)", marginTop: 4 }}>
            Export standard d'un flux Microsoft Power Automate (propriétés « actions » / « triggers »)
          </p>
        </div>

        {jsonContent && (
          <div className="row" style={{ justifyContent: "space-between", marginTop: "var(--space-4)" }}>
            <span className="row" style={{ gap: 6, fontSize: 12.5, color: "var(--color-muted)" }}>
              <FileJson size={15} /> {(jsonContent.length / 1024).toFixed(1)} Ko chargés
            </span>
            <Button onClick={handleImport} disabled={isImporting}>
              {isImporting ? "Vérification en cours…" : "Importer et vérifier"}
            </Button>
          </div>
        )}
      </div>

      {importError && (
        <div style={{ marginBottom: "var(--space-4)" }}>
          <Callout tone="error">{importError}</Callout>
        </div>
      )}

      {importResult && (
        <div className="card" style={{ padding: "var(--space-5)", marginBottom: "var(--space-5)" }}>
          <div className="row" style={{ gap: 10, marginBottom: 10 }}>
            {importResult.isValid ? (
              <CheckCircle2 size={18} color="var(--color-success)" />
            ) : (
              <XCircle size={18} color="var(--color-danger)" />
            )}
            <h3>{importResult.isValid ? "Flux valide" : "Flux invalide"}</h3>
          </div>

          {importResult.isValid ? (
            <p style={{ fontSize: 13.5, color: "var(--color-ink-soft)" }}>
              <strong>{importResult.name}</strong> — {importResult.actionsCount} action(s) détectée(s).
              Prêt pour la génération de documentation.
            </p>
          ) : (
            <Callout tone="error">{importResult.validationError}</Callout>
          )}

          {importResult.isValid && (
            <div style={{ marginTop: "var(--space-5)" }}>
              {pipelineStep && (
                <div style={{ marginBottom: "var(--space-4)" }}>
                  <PipelineTrail currentStepKey={pipelineStep} errorStepKey={generationError ? pipelineStep : null} />
                </div>
              )}
              {generationError && (
                <div style={{ marginBottom: "var(--space-3)" }}>
                  <Callout tone="error">{generationError}</Callout>
                </div>
              )}
              <Button onClick={handleGenerate} disabled={isGenerating}>
                {isGenerating ? "Génération en cours…" : "Lancer la génération de documentation"}
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
