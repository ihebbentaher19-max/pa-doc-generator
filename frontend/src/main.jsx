import React from "react";
import ReactDOM from "react-dom/client";
import { MsalProvider } from "@azure/msal-react";

import App from "./App";
import "./styles/global.css";
import { msalInstance } from "./services/msalInstance";

async function bootstrap() {
  await msalInstance.initialize();

  await msalInstance.handleRedirectPromise();

  ReactDOM.createRoot(document.getElementById("root")).render(
    <React.StrictMode>
      <MsalProvider instance={msalInstance}>
        <App />
      </MsalProvider>
    </React.StrictMode>
  );
}

bootstrap().catch((error) => {
  console.error(
    "Erreur d'initialisation Microsoft Authentication :",
    error
  );
});