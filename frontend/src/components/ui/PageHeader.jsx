export default function PageHeader({
  eyebrow,
  title,
  description,
  actions
}) {
  return (
    <header
      style={{
        marginBottom: "40px"
      }}
    >
      <div
        style={{
          display: "flex",

          justifyContent: "space-between",

          alignItems: "flex-start",

          gap: 20,

          flexWrap: "wrap"
        }}
      >
        <div>
          {eyebrow && (
            <div
              style={{
                color: "var(--color-primary)",

                textTransform: "uppercase",

                letterSpacing: ".08em",

                fontWeight: 700,

                fontSize: 12,

                marginBottom: 10
              }}
            >
              {eyebrow}
            </div>
          )}

          <h1
            style={{
              marginBottom: 10
            }}
          >
            {title}
          </h1>

          {description && (
            <p
              style={{
                color: "var(--color-muted)",

                maxWidth: 700,

                fontSize: 15,

                lineHeight: 1.6
              }}
            >
              {description}
            </p>
          )}
        </div>

        {actions && (
          <div
            style={{
              display: "flex",

              alignItems: "center",

              gap: 12,

              flexWrap: "wrap"
            }}
          >
            {actions}
          </div>
        )}
      </div>

      <div
        style={{
          marginTop: 28,

          height: 1,

          background: "var(--color-border)"
        }}
      />
    </header>
  );
}