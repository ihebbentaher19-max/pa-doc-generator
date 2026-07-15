const STATUS_CONFIG = {
  Brouillon: { label: "Brouillon", bg: "var(--color-archive-soft)", fg: "var(--color-archive)" },
  Valide: { label: "Validé", bg: "var(--color-success-soft)", fg: "var(--color-success)" },
  Archive: { label: "Archivé", bg: "var(--color-primary-soft)", fg: "var(--color-primary)" },
};

export default function StatusBadge({ status }) {
  const config = STATUS_CONFIG[status] || STATUS_CONFIG.Brouillon;
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 6,
        padding: "3px 10px",
        borderRadius: 999,
        fontSize: 12,
        fontWeight: 600,
        background: config.bg,
        color: config.fg,
      }}
    >
      <span style={{ width: 6, height: 6, borderRadius: "50%", background: "currentColor" }} />
      {config.label}
    </span>
  );
}
