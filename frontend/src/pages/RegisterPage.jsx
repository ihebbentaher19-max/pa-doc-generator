import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { Workflow } from "lucide-react";
import { useAuth } from "../context/useAuth";
import Button from "../components/ui/Button";
import Callout from "../components/ui/Callout";
import { getApiErrorMessage } from "../services/api";
import { inputStyle } from "../styles/formStyles";

export default function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await register(fullName, email, password);
      navigate("/");
    } catch (err) {
      setError(getApiErrorMessage(err, "Inscription impossible."));
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
          <h1 style={{ fontSize: 20 }}>Créer un compte</h1>
          <p style={{ color: "var(--color-muted)", fontSize: 13, textAlign: "center" }}>
            Le premier compte créé sur la plateforme obtient automatiquement le rôle Administrateur.
          </p>
        </div>

        {error && <Callout tone="error">{error}</Callout>}

        <label className="stack" style={{ gap: 6, fontSize: 13, fontWeight: 600 }}>
          Nom complet
          <input required value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Jane Doe" style={inputStyle} />
        </label>

        <label className="stack" style={{ gap: 6, fontSize: 13, fontWeight: 600 }}>
          Adresse e-mail
          <input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} placeholder="prenom.nom@entreprise.com" style={inputStyle} />
        </label>

        <label className="stack" style={{ gap: 6, fontSize: 13, fontWeight: 600 }}>
          Mot de passe
          <input type="password" required minLength={8} value={password} onChange={(e) => setPassword(e.target.value)} placeholder="8 caractères minimum" style={inputStyle} />
        </label>

        <Button type="submit" disabled={isSubmitting} style={{ marginTop: "var(--space-2)" }}>
          {isSubmitting ? "Création en cours…" : "Créer mon compte"}
        </Button>

        <p style={{ fontSize: 12.5, color: "var(--color-muted)", textAlign: "center" }}>
          Déjà un compte ?{" "}
          <Link to="/connexion" style={{ color: "var(--color-primary)", fontWeight: 600 }}>
            Se connecter
          </Link>
        </p>
      </form>
    </div>
  );
}
