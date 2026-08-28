import { LogLevel } from "@azure/msal-browser";

export const msalConfig = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID,
    authority: `https://login.microsoftonline.com/${
      import.meta.env.VITE_ENTRA_TENANT_ID
    }`,
    // Voir frontend/redirect.html : une page dédiée, hors de l'app React,
    // qui relaie la réponse d'authentification à la fenêtre principale.
    // Nécessaire avec @azure/msal-browser v5 pour loginPopup/acquireTokenPopup :
    // si redirectUri pointe vers l'app elle-même ("/"), React se monte à
    // l'intérieur du popup, crée sa propre instance MSAL et consomme le code
    // avant que la fenêtre principale ait pu le lire (le popup reste alors
    // bloqué sur une URL "#code=...").
    redirectUri:
      import.meta.env.VITE_ENTRA_REDIRECT_URI ||
      `${window.location.origin}/redirect.html`,
    postLogoutRedirectUri: window.location.origin,
    navigateToLoginRequestUrl: false,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            break;
          case LogLevel.Warning:
            console.warn(message);
            break;
          case LogLevel.Info:
            console.info(message);
            break;
          default:
            break;
        }
      },
    },
  },
};

export const loginRequest = {
  scopes: ["User.Read"],
};

export const powerPlatformRequest = {
  scopes: [
    "https://api.powerplatform.com/EnvironmentManagement.Environments.Read",
    "https://api.powerplatform.com/PowerAutomate.Flows.Read",
  ],
};