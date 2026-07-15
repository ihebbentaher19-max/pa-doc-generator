const TONES = {
  info: { bg: "var(--color-primary-soft)", fg: "var(--color-primary)" },
  error: { bg: "var(--color-danger-soft)", fg: "var(--color-danger)" },
  success: { bg: "var(--color-success-soft)", fg: "var(--color-success)" },
};

export default function Callout({ tone = "info", children }) {
  const { bg, fg } = TONES[tone] || TONES.info;
  return (
    <div
      style={{
        background: bg,
        color: fg,
        padding: "10px 14px",
        borderRadius: "var(--radius-md)",
        fontSize: 13.5,
        fontWeight: 500,
        lineHeight: 1.5,
      }}
    >
      {children}
    </div>
  );
}
