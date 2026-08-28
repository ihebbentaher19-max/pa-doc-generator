import React from "react";
import ReactDOM from "react-dom/client";
import { MsalProvider } from "@azure/msal-react";
import { msalInstance } from "./services/microsoftIdentityService";
import App from "./App";
import "./styles/global.css";

async function startApp() {
  // 1. Initialiser l'instance unique MSAL
  await msalInstance.initialize();

  // 2. Traiter la réponse de redirection (ferme la pop-up automatiquement)
  try {
    await msalInstance.handleRedirectPromise();
  } catch (error) {
    console.error("Erreur de redirection MSAL :", error);
  }

  // 3. Monter l'application React
  ReactDOM.createRoot(document.getElementById("root")).render(
    <React.StrictMode>
      <MsalProvider instance={msalInstance}>
        <App />
      </MsalProvider>
    </React.StrictMode>
  );
}

startApp();