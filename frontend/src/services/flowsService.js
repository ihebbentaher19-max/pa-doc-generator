import { apiClient } from "./api";

/** Module d'importation (cahier des charges, section 6). */
export async function importFlow(fileName, jsonContent) {
  const { data } = await apiClient.post("/flows/import", { fileName, jsonContent });
  return data;
}

export async function getFlowImport(id) {
  const { data } = await apiClient.get(`/flows/${id}`);
  return data;
}
