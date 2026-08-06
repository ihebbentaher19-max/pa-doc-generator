const STATUS_CONFIG = {
  Brouillon: {
    label: "Brouillon",
    bg: "#FFF4E5",
    fg: "#B26A00"
  },

  Valide: {
    label: "Validée",
    bg: "#E8F5E9",
    fg: "#107C10"
  },

  Archive: {
    label: "Archivée",
    bg: "#F3F2F1",
    fg: "#605E5C"
  }
};

export default function StatusBadge({ status }) {

  const config =
    STATUS_CONFIG[status] ||
    STATUS_CONFIG.Brouillon;

  return (

    <span
      style={{

        display: "inline-flex",

        alignItems: "center",

        gap: 8,

        padding: "6px 12px",

        borderRadius: 999,

        fontSize: 12,

        fontWeight: 600,

        background: config.bg,

        color: config.fg,

        border: "1px solid rgba(0,0,0,.05)"
      }}
    >

      <span
        style={{
          width: 8,
          height: 8,
          borderRadius: "50%",
          background: config.fg
        }}
      />

      {config.label}

    </span>
  );
}