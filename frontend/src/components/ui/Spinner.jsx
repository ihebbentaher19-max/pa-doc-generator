export default function Spinner({ size = 20 }) {
  return (
    <div
      style={{
        width: size,
        height: size,
        borderRadius: "50%",
        border: "3px solid #DCE6F5",
        borderTop: "3px solid var(--color-primary)",
        animation: "spin .8s linear infinite"
      }}
    />
  );
}
