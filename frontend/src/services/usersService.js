import { apiClient } from "./api";

/** Module de gestion des rôles - volet administration (section 6). */
export async function listUsers() {
  const { data } = await apiClient.get("/users");
  return data;
}

export async function changeUserRole(id, newRole) {
  await apiClient.patch(`/users/${id}/role`, { newRole });
}

export async function setUserActive(id, isActive) {
  await apiClient.patch(`/users/${id}/active`, { isActive });
}
