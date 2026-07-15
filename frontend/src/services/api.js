import axios from "axios";

// L'URL de l'API backend (ASP.NET Core Web API) est configurable via une
// variable d'environnement Vite, avec un repli sur le port de développement
// local par défaut (cf. backend/PADocGenerator.Api/Properties/launchSettings.json).
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5080/api";

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: { "Content-Type": "application/json" },
});

const TOKEN_STORAGE_KEY = "padocgen_token";

export function getStoredToken() {
  return localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function setStoredToken(token) {
  if (token) localStorage.setItem(TOKEN_STORAGE_KEY, token);
  else localStorage.removeItem(TOKEN_STORAGE_KEY);
}

apiClient.interceptors.request.use((config) => {
  const token = getStoredToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      setStoredToken(null);
      if (!window.location.pathname.startsWith("/connexion")) {
        window.location.href = "/connexion";
      }
    }
    return Promise.reject(error);
  }
);

/** Extrait un message d'erreur lisible depuis une réponse API en erreur. */
export function getApiErrorMessage(error, fallback = "Une erreur est survenue.") {
  return error?.response?.data?.message || error?.message || fallback;
}
