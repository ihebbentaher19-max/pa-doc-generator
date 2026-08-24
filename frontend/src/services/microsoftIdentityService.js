import {
  InteractionRequiredAuthError,
} from "@azure/msal-browser";

import { msalInstance } from "./msalInstance";
import { loginRequest } from "../config/authConfig";

export async function loginToMicrosoft() {
  try {
    const existingAccounts = msalInstance.getAllAccounts();

    if (existingAccounts.length > 0) {
      msalInstance.setActiveAccount(existingAccounts[0]);

      return {
        account: existingAccounts[0],
        connected: true,
      };
    }

    const result = await msalInstance.loginPopup(loginRequest);

    if (result?.account) {
      msalInstance.setActiveAccount(result.account);
    }

    return {
      account: result.account,
      connected: true,
    };
  } catch (error) {
    console.error("Connexion Microsoft :", error);
    throw error;
  }
}

export function getMicrosoftConnectionStatus() {
  const account =
    msalInstance.getActiveAccount() ||
    msalInstance.getAllAccounts()[0] ||
    null;

  return {
    configured: true,
    connected: Boolean(account),
    account,
  };
}

export async function getPowerPlatformAccessToken() {
  const account =
    msalInstance.getActiveAccount() ||
    msalInstance.getAllAccounts()[0];

  if (!account) {
    throw new Error(
      "Connectez-vous à Microsoft 365 avant d'accéder aux flux Power Automate."
    );
  }

  const request = {
    ...loginRequest,
    account,
  };

  try {
    const result = await msalInstance.acquireTokenSilent(request);

    return result.accessToken;
  } catch (error) {
    if (error instanceof InteractionRequiredAuthError) {
      const result = await msalInstance.acquireTokenPopup(request);

      return result.accessToken;
    }

    throw error;
  }
}

export async function getDataverseAccessToken(dataverseUrl) {
  if (!dataverseUrl) {
    throw new Error(
      "L'URL de l'environnement Dataverse est obligatoire."
    );
  }

  const resource = new URL(dataverseUrl).origin;

  const account =
    msalInstance.getActiveAccount() ||
    msalInstance.getAllAccounts()[0];

  if (!account) {
    throw new Error(
      "Connectez-vous à Microsoft 365 avant d'accéder à Dataverse."
    );
  }

  const request = {
    scopes: [`${resource}/user_impersonation`],
    account,
  };

  try {
    const result = await msalInstance.acquireTokenSilent(request);

    return result.accessToken;
  } catch (error) {
    if (error instanceof InteractionRequiredAuthError) {
      const result = await msalInstance.acquireTokenPopup(request);

      return result.accessToken;
    }

    throw error;
  }
}

export function disconnectMicrosoft() {
  const account =
    msalInstance.getActiveAccount() ||
    msalInstance.getAllAccounts()[0];

  if (account) {
    msalInstance.logoutPopup({
      account,
      mainWindowRedirectUri: window.location.origin,
    });
  }
}