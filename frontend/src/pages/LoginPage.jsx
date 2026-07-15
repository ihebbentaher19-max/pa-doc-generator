import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { Workflow } from "lucide-react";
import { useAuth } from "../context/useAuth";
import Button from "../components/ui/Button";
import Callout from "../components/ui/Callout";
import { getApiErrorMessage } from "../services/api";
import { inputStyle } from "../styles/formStyles";

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login(email, password);
      navigate("/");
    } catch (err) {
      setError(getApiErrorMessage(err, "Connexion impossible. Vérifiez vos identifiants."));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div
      style={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "var(--color-bg)",
        padding: "var(--space-4)",
      }}
    >
      <form onSubmit={handleSubmit} className="card stack" style={{ width: 380, padding: "var(--space-6)", gap: "var(--space-4)" }}>
        <div className="stack" style={{ alignItems: "center", gap: "var(--space-2)", marginBottom: "var(--space-2)" }}>
          <div
            style={{
              width: 44, height: 44, borderRadius: 12, background: "var(--color-primary)",
              display: "flex", alignItems: "center", justifyContent: "center", color: "#fff",
            }}
          >
            <Workflow size={22} />
          </div>
          <h1 style={{ fontSize: 20 }}>Connexion</h1>
          <p style={{ color: "var(--color-muted)", fontSize: 13, textAlign: "center" }}>
            Générateur de documentation IA pour Power Automate
          </p>
        </div>

        {error && <Callout tone="error">{error}</Callout>}

        <label className="stack" style={{ gap: 6, fontSize: 13, fontWeight: 600 }}>
          Adresse e-mail
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="prenom.nom@entreprise.com"
            style={inputStyle}
          />
        </label>

        <label className="stack" style={{ gap: 6, fontSize: 13, fontWeight: 600 }}>
          Mot de passe
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            style={inputStyle}
          />
        </label>

        <Button type="submit" disabled={isSubmitting} style={{ marginTop: "var(--space-2)" }}>
          {isSubmitting ? "Connexion en cours…" : "Se connecter"}
        </Button>

        <p style={{ fontSize: 12.5, color: "var(--color-muted)", textAlign: "center" }}>
          Pas encore de compte ?{" "}
          <Link to="/inscription" style={{ color: "var(--color-primary)", fontWeight: 600 }}>
            Créer un compte
          </Link>
        </p>
      </form>
    </div>
  );
}

