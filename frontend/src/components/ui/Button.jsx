const VARIANTS = {
  primary: {
    background: "linear-gradient(135deg,#0078D4,#0094FF)",
    color: "#ffffff",
    border: "1px solid #0078D4",
    boxShadow: "0 6px 18px rgba(0,120,212,.20)",
  },

  secondary: {
    background: "#ffffff",
    color: "var(--color-primary)",
    border: "1px solid var(--color-border)",
    boxShadow: "0 2px 8px rgba(0,0,0,.04)",
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

export default function Button({
  variant = "primary",
  disabled = false,
  children,
  style,
  ...props
}) {
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

        minHeight: 42,

        padding: "10px 18px",

        borderRadius: 10,

        fontSize: 14,

        fontWeight: 600,

        cursor: disabled ? "not-allowed" : "pointer",

        opacity: disabled ? .55 : 1,

        transition:
          "all .18s ease",

        outline: "none",

        ...style,
      }}

      onMouseEnter={(e) => {
        if (disabled) return;

        e.currentTarget.style.transform = "translateY(-1px)";
        e.currentTarget.style.filter = "brightness(.98)";
      }}

      onMouseLeave={(e) => {
        e.currentTarget.style.transform = "translateY(0)";
        e.currentTarget.style.filter = "brightness(1)";
      }}

      onMouseDown={(e) => {
        if (disabled) return;

        e.currentTarget.style.transform = "scale(.98)";
      }}

      onMouseUp={(e) => {
        e.currentTarget.style.transform = "translateY(-1px)";
      }}

      {...props}
    >
      {children}
    </button>
  );
}