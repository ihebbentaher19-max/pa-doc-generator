// Authentification OAuth 2.0 / PKCE sans secret côté navigateur. L'application
// Entra doit être déclarée comme SPA avec l'URL de redirection configurée.
const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID;
const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID || "organizations";
const redirectUri = import.meta.env.VITE_ENTRA_REDIRECT_URI || window.location.origin;
const authority = `https://login.microsoftonline.com/${encodeURIComponent(tenantId)}/oauth2/v2.0`;

const accessTokens = new Map();

export function isMicrosoftIdentityConfigured() {
  return Boolean(clientId && !clientId.includes("CHANGE_ME"));
}

export async function getPowerPlatformAccessToken() {
  return acquireToken("power-platform", ["https://api.powerplatform.com/.default"]);
}

export async function getDataverseAccessToken(dataverseUrl) {
  const resource = new URL(dataverseUrl).origin;
  return acquireToken(`dataverse:${resource}`, [`${resource}/user_impersonation`]);
}

async function acquireToken(cacheKey, scopes) {
  if (!isMicrosoftIdentityConfigured()) {
    throw new Error("La connexion Microsoft 365 n'est pas configurée. Renseignez VITE_ENTRA_CLIENT_ID dans le fichier .env du frontend.");
  }

  const cached = accessTokens.get(cacheKey);
  if (cached && cached.expiresAt > Date.now() + 60_000) return cached.accessToken;

  const authorizationCode = await requestAuthorizationCode(scopes);
  const tokenResponse = await exchangeCode(authorizationCode.code, authorizationCode.verifier, scopes);
  const expiresAt = Date.now() + Number(tokenResponse.expires_in || 3600) * 1000;
  accessTokens.set(cacheKey, { accessToken: tokenResponse.access_token, expiresAt });
  return tokenResponse.access_token;
}

function requestAuthorizationCode(scopes) {
  return new Promise((resolve, reject) => {
    const state = randomUrlSafeValue();
    const verifier = randomUrlSafeValue();
    sha256Base64Url(verifier).then((challenge) => {
    const params = new URLSearchParams({
      client_id: clientId,
      response_type: "code",
      redirect_uri: redirectUri,
      response_mode: "query",
      scope: ["openid", "profile", ...scopes].join(" "),
      state,
      code_challenge: challenge,
      code_challenge_method: "S256",
    });

    const popup = window.open(
      `${authority}/authorize?${params.toString()}`,
      "padocgenerator-microsoft-login",
      "popup=yes,width=560,height=720,resizable=yes,scrollbars=yes"
    );
    if (!popup) {
      reject(new Error("La fenêtre de connexion Microsoft a été bloquée par le navigateur."));
      return;
    }

    const timeout = window.setTimeout(cleanup, 120_000);
    const closeWatcher = window.setInterval(() => {
      if (popup.closed) {
        cleanup();
        reject(new Error("La connexion Microsoft a été annulée."));
      }
    }, 500);

    function onMessage(event) {
      if (event.origin !== window.location.origin || event.data?.type !== "padocgenerator-entra-callback") return;
      const { code, returnedState, error, errorDescription } = event.data;
      if (returnedState !== state) return;
      cleanup();
      if (error) reject(new Error(errorDescription || "Microsoft a refusé la connexion."));
      else if (code) resolve({ code, verifier });
      else reject(new Error("La réponse de connexion Microsoft est incomplète."));
    }

    function cleanup() {
      window.removeEventListener("message", onMessage);
      window.clearTimeout(timeout);
      window.clearInterval(closeWatcher);
      if (!popup.closed) popup.close();
    }

    window.addEventListener("message", onMessage);
    }).catch(reject);
  });
}

async function exchangeCode(code, verifier, scopes) {
  const body = new URLSearchParams({
    client_id: clientId,
    grant_type: "authorization_code",
    code,
    redirect_uri: redirectUri,
    code_verifier: verifier,
    scope: ["openid", "profile", ...scopes].join(" "),
  });
  const response = await fetch(`${authority}/token`, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body,
  });
  const payload = await response.json();
  if (!response.ok || !payload.access_token) {
    throw new Error(payload.error_description || "Impossible d'obtenir le jeton Microsoft demandé.");
  }
  return payload;
}

function randomUrlSafeValue() {
  const bytes = new Uint8Array(32);
  window.crypto.getRandomValues(bytes);
  return base64Url(bytes);
}

async function sha256Base64Url(value) {
  const hash = await window.crypto.subtle.digest("SHA-256", new TextEncoder().encode(value));
  return base64Url(new Uint8Array(hash));
}

function base64Url(bytes) {
  let binary = "";
  bytes.forEach((byte) => { binary += String.fromCharCode(byte); });
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

// Le popup revient vers le même SPA. Cette fonction transmet uniquement le code
// d'autorisation à la fenêtre parente ; le jeton est échangé par la fenêtre mère.
export function handleMicrosoftPopupCallback() {
  const params = new URLSearchParams(window.location.search);
  if (!window.opener || (!params.has("code") && !params.has("error"))) return false;

  window.opener.postMessage({
    type: "padocgenerator-entra-callback",
    code: params.get("code"),
    returnedState: params.get("state"),
    error: params.get("error"),
    errorDescription: params.get("error_description"),
  }, window.location.origin);
  window.close();
  return true;
}
