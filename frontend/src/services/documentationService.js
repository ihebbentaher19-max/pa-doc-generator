import { apiClient } from "./api";

/** Module de génération + mise en forme + gestion documentaire (section 6). */
export async function generateDocumentation(flowImportId) {
  const { data } = await apiClient.post("/documentation/generate", { flowImportId });
  return data;
}

export async function getDocumentation(id) {
  const { data } = await apiClient.get(`/documentation/${id}`);
  return data;
}

export async function updateDocumentation(id, title, content, changeNote) {
  const { data } = await apiClient.put(`/documentation/${id}`, { title, content, changeNote });
  return data;
}

export async function changeDocumentationStatus(id, newStatus) {
  const { data } = await apiClient.patch(`/documentation/${id}/status`, { newStatus });
  return data;
}

export async function getVersionHistory(id) {
  const { data } = await apiClient.get(`/documentation/${id}/versions`);
  return data;
}

export async function deleteDocumentation(id) {
  await apiClient.delete(`/documentation/${id}`);
}

/** Module de recherche et consultation (section 6). */
export async function searchDocumentation({ keyword = "", status = "", page = 1, pageSize = 20 }) {
  const { data } = await apiClient.get("/documentation/search", {
    params: { keyword: keyword || undefined, status: status || undefined, page, pageSize },
  });
  return data;
}

/** Module d'export (section 6). */
export function getExportUrl(id, format) {
  const base = apiClient.defaults.baseURL;
  return `${base}/documentation/${id}/export/${format}`;
}

export async function downloadExport(id, format, suggestedFileName) {
  const response = await apiClient.get(`/documentation/${id}/export/${format}`, { responseType: "blob" });
  const blobUrl = window.URL.createObjectURL(new Blob([response.data]));
  const link = document.createElement("a");
  link.href = blobUrl;
  link.download = suggestedFileName || `documentation.${format === "word" ? "docx" : "pdf"}`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(blobUrl);
}
