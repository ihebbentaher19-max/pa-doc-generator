import { PublicClientApplication } from "@azure/msal-browser";
import { msalConfig, powerPlatformRequest } from "../config/authConfig";

export const msalInstance = new PublicClientApplication(msalConfig);

export async function loginToMicrosoft() {
  try {
    const loginResponse = await msalInstance.loginPopup(powerPlatformRequest);

    if (loginResponse.account) {
      msalInstance.setActiveAccount(loginResponse.account);
    }

    return loginResponse;
  } catch (error) {
    console.error("Erreur lors de la connexion Microsoft :", error);
    throw error;
  }
}

export function getMicrosoftConnectionStatus() {
  const activeAccount = msalInstance.getActiveAccount();
  const accounts = msalInstance.getAllAccounts();
  const account = activeAccount || (accounts.length > 0 ? accounts[0] : null);

  return {
    connected: !!account,
    account: account,
  };
}

export async function getPowerPlatformAccessToken() {
  const account =
    msalInstance.getActiveAccount() || msalInstance.getAllAccounts()[0];

  if (!account) {
    throw new Error("Aucun compte Microsoft connecté.");
  }

  try {
    const response = await msalInstance.acquireTokenSilent({
      ...powerPlatformRequest,
      account: account,
    });
    return response.accessToken;
  } catch (error) {
    const response = await msalInstance.acquireTokenPopup({
      ...powerPlatformRequest,
      account: account,
    });
    return response.accessToken;
  }
}

export async function getDataverseAccessToken(dataverseUrl) {
  const account =
    msalInstance.getActiveAccount() || msalInstance.getAllAccounts()[0];

  if (!account) {
    throw new Error("Aucun compte Microsoft connecté.");
  }

  const baseUrl = dataverseUrl
    ? dataverseUrl.replace(/\/$/, "")
    : "https://admin.services.crm.dynamics.com";

  const request = {
    scopes: [`${baseUrl}/user_impersonation`],
    account: account,
  };

  try {
    const response = await msalInstance.acquireTokenSilent(request);
    return response.accessToken;
  } catch (error) {
    const response = await msalInstance.acquireTokenPopup(request);
    return response.accessToken;
  }
}