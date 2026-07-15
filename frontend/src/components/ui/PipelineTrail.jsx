const STEPS = [
  { key: "import", label: "Import" },
  { key: "lecture", label: "Lecture & préparation" },
  { key: "generation", label: "Génération IA" },
  { key: "mise-en-forme", label: "Mise en forme" },
  { key: "enregistrement", label: "Enregistrement" },
];

/**
 * Élément signature de l'interface : matérialise le pipeline exact décrit en
 * section 6 du cahier des charges (module d'importation -> lecture/préparation
 * -> génération -> mise en forme -> gestion documentaire), pour que l'utilisateur
 * comprenne concrètement ce que fait la plateforme pendant la génération.
 */
export default function PipelineTrail({ currentStepKey, errorStepKey }) {
  const currentIndex = STEPS.findIndex((s) => s.key === currentStepKey);

  return (
    <div className="row" style={{ gap: 0 }}>
      {STEPS.map((step, index) => {
        const isDone = index < currentIndex;
        const isCurrent = index === currentIndex;
        const isError = step.key === errorStepKey;

        let dotColor = "var(--color-border)";
        let textColor = "var(--color-muted)";
        if (isDone) {
          dotColor = "var(--color-success)";
          textColor = "var(--color-ink-soft)";
        }
        if (isCurrent) {
          dotColor = "var(--color-primary)";
          textColor = "var(--color-primary)";
        }
        if (isError) {
          dotColor = "var(--color-danger)";
          textColor = "var(--color-danger)";
        }

        return (
          <div key={step.key} style={{ display: "flex", alignItems: "center", flex: index < STEPS.length - 1 ? 1 : "none" }}>
            <div className="stack" style={{ alignItems: "center", gap: 6, minWidth: 64 }}>
              <div
                style={{
                  width: 12,
                  height: 12,
                  borderRadius: "50%",
                  background: dotColor,
                  boxShadow: isCurrent ? `0 0 0 4px ${isError ? "var(--color-danger-soft)" : "var(--color-primary-soft)"}` : "none",
                  transition: "background 200ms ease, box-shadow 200ms ease",
                }}
              />
              <span style={{ fontSize: 11, fontWeight: 600, color: textColor, textAlign: "center", whiteSpace: "nowrap" }}>
                {step.label}
              </span>
            </div>
            {index < STEPS.length - 1 && (
              <div
                style={{
                  flex: 1,
                  height: 2,
                  marginBottom: 18,
                  background: index < currentIndex ? "var(--color-success)" : "var(--color-border)",
                  transition: "background 200ms ease",
                }}
              />
            )}
          </div>
        );
      })}
    </div>
  );
}
