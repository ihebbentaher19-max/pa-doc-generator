import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { msalInstance, loginRequest } from "../config/msal";

export async function loginMicrosoft() {
  const accounts = msalInstance.getAllAccounts();

  if (accounts.length === 0) {
    const loginResponse = await msalInstance.loginPopup(loginRequest);

    if (loginResponse.account) {
      msalInstance.setActiveAccount(loginResponse.account);
    }
  } else {
    msalInstance.setActiveAccount(accounts[0]);
  }

  return getPowerPlatformAccessToken();
}

export async function getPowerPlatformAccessToken() {
  let account = msalInstance.getActiveAccount();

  if (!account) {
    const accounts = msalInstance.getAllAccounts();

    if (accounts.length === 0) {
      throw new Error("Vous devez vous connecter à Microsoft 365.");
    }

    account = accounts[0];
    msalInstance.setActiveAccount(account);
  }

  const request = {
    ...loginRequest,
    account,
  };

  try {
    const response = await msalInstance.acquireTokenSilent(request);

    return response.accessToken;
  } catch (error) {
    if (error instanceof InteractionRequiredAuthError) {
      const response = await msalInstance.acquireTokenPopup(request);
      return response.accessToken;
    }

    throw error;
  }
}