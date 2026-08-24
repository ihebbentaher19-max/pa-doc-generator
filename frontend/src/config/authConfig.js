import { LogLevel } from "@azure/msal-browser";

export const msalConfig = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID,
    authority: `https://login.microsoftonline.com/${import.meta.env.VITE_ENTRA_TENANT_ID}`,
    redirectUri:
      import.meta.env.VITE_ENTRA_REDIRECT_URI ||
      window.location.origin,
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
  scopes: [
    "https://api.powerplatform.com/PowerAutomate.Flows.Read",
  ],
};