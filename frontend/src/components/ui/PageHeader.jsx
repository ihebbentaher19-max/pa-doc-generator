export default function PageHeader({ eyebrow, title, description, actions }) {
  return (
    <header
      style={{
        display: "flex",
        alignItems: "flex-start",
        justifyContent: "space-between",
        gap: "var(--space-4)",
        marginBottom: "var(--space-6)",
        flexWrap: "wrap",
      }}
    >
      <div className="stack" style={{ gap: 6 }}>
        {eyebrow && (
          <span style={{ fontSize: 11.5, fontWeight: 700, letterSpacing: "0.06em", textTransform: "uppercase", color: "var(--color-primary)" }}>
            {eyebrow}
          </span>
        )}
        <h1>{title}</h1>
        {description && <p style={{ color: "var(--color-muted)", fontSize: 13.5, maxWidth: 560 }}>{description}</p>}
      </div>
      {actions && <div className="row" style={{ gap: 10 }}>{actions}</div>}
    </header>
  );
}
