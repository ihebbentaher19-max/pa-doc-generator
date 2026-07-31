import { apiClient, setStoredToken, setStoredUser } from "./api";

function storeSession(data) {
  setStoredToken(data.token);
  setStoredUser({ id: data.id, fullName: data.fullName, role: data.role });
}

export async function login(email, password) {
  const { data } = await apiClient.post("/auth/login", { email, password });
  storeSession(data);
  return data;
}

export async function register(fullName, email, password) {
  const { data } = await apiClient.post("/auth/register", { fullName, email, password });
  storeSession(data);
  return data;
}

export function logout() {
  setStoredToken(null);
  setStoredUser(null);
}
