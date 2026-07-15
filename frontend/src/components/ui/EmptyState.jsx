export default function EmptyState({ icon: Icon, title, description, action }) {
  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        textAlign: "center",
        gap: 10,
        padding: "56px 24px",
        color: "var(--color-muted)",
      }}
    >
      {Icon && (
        <div
          style={{
            width: 44,
            height: 44,
            borderRadius: 12,
            background: "var(--color-primary-soft)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            color: "var(--color-primary)",
            marginBottom: 4,
          }}
        >
          <Icon size={22} />
        </div>
      )}
      <h3 style={{ color: "var(--color-ink)" }}>{title}</h3>
      {description && <p style={{ maxWidth: 380, fontSize: 13.5 }}>{description}</p>}
      {action}
    </div>
  );
}
