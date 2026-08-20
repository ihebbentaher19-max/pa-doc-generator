import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import App from "./App.jsx";
import "./styles/global.css";
import { handleMicrosoftPopupCallback } from "./services/microsoftIdentityService.js";

if (!handleMicrosoftPopupCallback()) {
  createRoot(document.getElementById("root")).render(
    <StrictMode>
      <App />
    </StrictMode>
  );
}
