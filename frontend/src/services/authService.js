import { apiClient, setStoredToken } from "./api";

export async function login(email, password) {
  const { data } = await apiClient.post("/auth/login", { email, password });
  setStoredToken(data.token);
  return data;
}

export async function register(fullName, email, password) {
  const { data } = await apiClient.post("/auth/register", { fullName, email, password });
  setStoredToken(data.token);
  return data;
}

export function logout() {
  setStoredToken(null);
}
