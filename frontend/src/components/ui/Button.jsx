const VARIANTS = {
  primary: {
    background: "var(--color-primary)",
    color: "#fff",
    border: "1px solid var(--color-primary)",
  },
  secondary: {
    background: "var(--color-surface)",
    color: "var(--color-ink)",
    border: "1px solid var(--color-border)",
  },
  ghost: {
    background: "transparent",
    color: "var(--color-ink-soft)",
    border: "1px solid transparent",
  },
  danger: {
    background: "var(--color-danger-soft)",
    color: "var(--color-danger)",
    border: "1px solid transparent",
  },
};

export default function Button({ variant = "primary", disabled, children, style, ...props }) {
  const variantStyle = VARIANTS[variant] || VARIANTS.primary;
  return (
    <button
      disabled={disabled}
      style={{
        ...variantStyle,
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        gap: 8,
        padding: "9px 16px",
        borderRadius: "var(--radius-sm)",
        fontSize: 13.5,
        fontWeight: 600,
        opacity: disabled ? 0.55 : 1,
        cursor: disabled ? "not-allowed" : "pointer",
        transition: "filter 120ms ease, transform 80ms ease",
        ...style,
      }}
      onMouseDown={(e) => !disabled && (e.currentTarget.style.transform = "scale(0.98)")}
      onMouseUp={(e) => (e.currentTarget.style.transform = "scale(1)")}
      {...props}
    >
      {children}
    </button>
  );
}
