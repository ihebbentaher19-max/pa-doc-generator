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

export async function getPowerPlatformEnvironments(powerPlatformAccessToken) {
  const { data } = await apiClient.get("/flows/power-platform/environments", {
    headers: { "X-PowerPlatform-Access-Token": powerPlatformAccessToken },
  });
  return data;
}

export async function getPowerPlatformFlows(environmentId, powerPlatformAccessToken) {
  const { data } = await apiClient.get(`/flows/power-platform/environments/${encodeURIComponent(environmentId)}/flows`, {
    headers: { "X-PowerPlatform-Access-Token": powerPlatformAccessToken },
  });
  return data;
}

export async function importPowerPlatformFlow({ environmentId, workflowId, powerPlatformAccessToken, dataverseAccessToken }) {
  const { data } = await apiClient.post("/flows/import/power-platform", { environmentId, workflowId }, {
    headers: {
      "X-PowerPlatform-Access-Token": powerPlatformAccessToken,
      "X-Dataverse-Access-Token": dataverseAccessToken,
    },
  });
  return data;
}
