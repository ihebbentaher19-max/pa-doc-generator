export default function EmptyState({
  icon: Icon,
  title,
  description,
  action
}) {
  return (
    <div
      style={{
        padding: "60px 32px",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        textAlign: "center",
        borderRadius: 20,
        background: "linear-gradient(180deg,#FFFFFF,#F8FAFC)",
        border: "1px dashed var(--color-border)"
      }}
    >
      {Icon && (
        <div
          style={{
            width: 72,
            height: 72,
            borderRadius: 20,
            background: "var(--color-primary-soft)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            color: "var(--color-primary)",
            marginBottom: 22
          }}
        >
          <Icon size={36} />
        </div>
      )}

      <h3
        style={{
          marginBottom: 12
        }}
      >
        {title}
      </h3>

      {description && (
        <p
          style={{
            maxWidth: 520,
            color: "var(--color-muted)",
            fontSize: 15,
            lineHeight: 1.7,
            marginBottom: 26
          }}
        >
          {description}
        </p>
      )}

      {action}
    </div>
  );
}