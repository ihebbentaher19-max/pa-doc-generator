import { apiClient } from "./api";

/** Module de tableau de bord (section 6). */
export async function getDashboardStats() {
  const { data } = await apiClient.get("/dashboard");
  return data;
}
