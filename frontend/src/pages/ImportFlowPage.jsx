import { useCallback, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { UploadCloud, FileJson, CheckCircle2, XCircle, Cloud, LogIn, RefreshCw } from "lucide-react";
import PageHeader from "../components/ui/PageHeader";
import Button from "../components/ui/Button";
import Callout from "../components/ui/Callout";
import PipelineTrail from "../components/ui/PipelineTrail";
import {
  getPowerPlatformEnvironments,
  getPowerPlatformFlows,
  importFlow,
  importPowerPlatformFlow,
} from "../services/flowsService";
import { generateDocumentation } from "../services/documentationService";
import { getApiErrorMessage } from "../services/api";
import {
  loginToMicrosoft,
  getMicrosoftConnectionStatus,
} from "../services/microsoftIdentityService";
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

  const [isMicrosoftConnected, setIsMicrosoftConnected] = useState(false);
  const [environments, setEnvironments] = useState([]);
  const [selectedEnvironmentId, setSelectedEnvironmentId] = useState("");
  const [flows, setFlows] = useState([]);
  const [selectedWorkflowId, setSelectedWorkflowId] = useState("");

  const [isConnectingMicrosoft, setIsConnectingMicrosoft] = useState(false);
  const [isLoadingFlows, setIsLoadingFlows] = useState(false);
  const [isImportingPowerPlatform, setIsImportingPowerPlatform] = useState(false);
  const [powerPlatformError, setPowerPlatformError] = useState(null);
  const [selectedEnvironment, setSelectedEnvironment] = useState(null);
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

  async function handleMicrosoftConnect() {
    setIsConnectingMicrosoft(true);
    setPowerPlatformError(null);
    setImportResult(null);

    try {
      const loginResult = await loginToMicrosoft();

      if (!loginResult?.connected) {
        throw new Error(
          "La connexion Microsoft 365 n'a pas pu être établie."
        );
      }

      const availableEnvironments =
        await getPowerPlatformEnvironments();

      setIsMicrosoftConnected(true);
      setEnvironments(availableEnvironments);
      setSelectedEnvironmentId("");
      setFlows([]);
      setSelectedWorkflowId("");

      if (!availableEnvironments.length) {
        setPowerPlatformError(
          "Aucun environnement Power Platform accessible n'a été trouvé pour ce compte."
        );
      }
    } catch (err) {
      console.error("Connexion Microsoft :", err);

      setIsMicrosoftConnected(false);

      setPowerPlatformError(
        getApiErrorMessage(
          err,
          "Impossible de se connecter à Microsoft 365 ou de charger les environnements."
        )
      );
    } finally {
      setIsConnectingMicrosoft(false);
    }
  }

  async function handleEnvironmentChange(environmentId) {
    setSelectedEnvironmentId(environmentId);
    setSelectedWorkflowId("");
    setFlows([]);
    setPowerPlatformError(null);

    const environment =
      environments.find(
        (item) => item.id === environmentId
      ) || null;

    setSelectedEnvironment(environment);

    if (!environmentId || !isMicrosoftConnected) return;

    setIsLoadingFlows(true);

    try {
      const availableFlows =
        await getPowerPlatformFlows(environmentId);

      setFlows(availableFlows);

      if (!availableFlows.length) {
        setPowerPlatformError(
          "Aucun flux cloud accessible n'a été trouvé dans cet environnement."
        );
      }
    } catch (err) {
      setPowerPlatformError(
        getApiErrorMessage(
          err,
          "Impossible de charger les flux de cet environnement."
        )
      );
    } finally {
      setIsLoadingFlows(false);
    }
  }

  async function handlePowerPlatformImport() {
    if (
      !selectedEnvironmentId ||
      !selectedWorkflowId ||
      !isMicrosoftConnected
    ) {
      return;
    }

    setIsImportingPowerPlatform(true);
    setPowerPlatformError(null);
    setImportResult(null);

    try {
      const selectedEnvironment = environments.find(
        (environment) =>
          environment.id === selectedEnvironmentId
      );

      if (!selectedEnvironment) {
        throw new Error(
          "L'environnement Power Platform sélectionné est introuvable."
        );
      }

      const result = await importPowerPlatformFlow({
        environmentId: selectedEnvironmentId,
        workflowId: selectedWorkflowId,
        dataverseUrl: selectedEnvironment.url,
      });

      setImportResult(result);
    } catch (err) {
      setPowerPlatformError(
        getApiErrorMessage(
          err,
          "L'import du flux Power Platform a échoué."
        )
      );
    } finally {
      setIsImportingPowerPlatform(false);
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
        description="Sélectionnez un flux auquel vous avez accès dans Microsoft 365, ou chargez un export JSON. La plateforme vérifie sa conformité avant de lancer la génération."
      />

      <div className="card" style={{ padding: "var(--space-5)", marginBottom: "var(--space-5)" }}>
        <div className="row" style={{ gap: 10, marginBottom: 8 }}>
          <Cloud size={20} color="var(--color-primary)" />
          <h3>Importer depuis Microsoft 365 / Power Platform</h3>
        </div>
        <p style={{ fontSize: 13.5, color: "var(--color-ink-soft)", marginBottom: "var(--space-4)" }}>
          Connectez votre compte Microsoft, choisissez l'environnement puis le flux cloud à documenter. Seuls les flux auxquels votre compte a accès sont affichés.
        </p>
          <>
            <Button
              onClick={handleMicrosoftConnect}
              disabled={isConnectingMicrosoft}
              variant="secondary"
            >
              {isConnectingMicrosoft ? (
                "Connexion Microsoft en cours…"
              ) : (
                <>
                  <LogIn size={16} />
                  {isMicrosoftConnected
                    ? "Actualiser les environnements"
                    : "Se connecter à Microsoft 365"}
                </>
              )}
            </Button>

            {environments.length > 0 && (
              <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(230px, 1fr))", gap: "var(--space-3)", marginTop: "var(--space-4)" }}>
                <label className="stack" style={{ gap: 6, fontWeight: 600, fontSize: 13 }}>
                  Environnement Power Platform
                  <select value={selectedEnvironmentId} onChange={(e) => handleEnvironmentChange(e.target.value)} style={{ padding: "10px 12px", border: "1px solid var(--color-border)", borderRadius: "var(--radius-sm)", background: "white" }}>
                    <option value="">Sélectionnez un environnement</option>
                    {environments.map((environment) => (
                      <option key={environment.id} value={environment.id}>
                        {environment.displayName}{environment.type ? ` — ${environment.type}` : ""}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="stack" style={{ gap: 6, fontWeight: 600, fontSize: 13 }}>
                  Flux cloud
                  <select value={selectedWorkflowId} onChange={(e) => setSelectedWorkflowId(e.target.value)} disabled={!selectedEnvironmentId || isLoadingFlows} style={{ padding: "10px 12px", border: "1px solid var(--color-border)", borderRadius: "var(--radius-sm)", background: "white" }}>
                    <option value="">{isLoadingFlows ? "Chargement des flux…" : "Sélectionnez un flux"}</option>
                    {flows.map((flow) => (
                      <option key={flow.workflowId} value={flow.workflowId}>
                        {flow.displayName}{flow.state ? ` — ${flow.state}` : ""}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            )}

            {selectedWorkflowId && (
              <div style={{ marginTop: "var(--space-4)" }}>
                <Button onClick={handlePowerPlatformImport} disabled={isImportingPowerPlatform}>
                  {isImportingPowerPlatform ? "Import du flux en cours…" : <><RefreshCw size={16} /> Importer le flux sélectionné</>}
                </Button>
              </div>
            )}
          </>

        {powerPlatformError && <div style={{ marginTop: "var(--space-4)" }}><Callout tone="error">{powerPlatformError}</Callout></div>}
      </div>

      <div className="card" style={{ padding: "var(--space-5)", marginBottom: "var(--space-5)" }}>
        <p style={{ fontWeight: 600, marginBottom: "var(--space-3)" }}>Ou importer un fichier JSON</p>
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
