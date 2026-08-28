import { apiClient } from "./api";

import {
  getPowerPlatformAccessToken,
  getDataverseAccessToken,
} from "./microsoftIdentityService";

export async function importFlow(fileName, jsonContent) {
  const { data } = await apiClient.post("/flows/import", {
    fileName,
    jsonContent,
  });

  return data;
}

export async function getFlowImport(id) {
  const { data } = await apiClient.get(`/flows/${id}`);

  return data;
}

export async function getPowerPlatformEnvironments() {
  const accessToken =
    await getPowerPlatformAccessToken();

  const { data } = await apiClient.get(
    "/flows/power-platform/environments",
    {
      headers: {
        "X-PowerPlatform-Access-Token": accessToken,
      },
    }
  );

  return data;
}

export async function getPowerPlatformFlows(environmentId) {
  const accessToken =
    await getPowerPlatformAccessToken();

  const { data } = await apiClient.get(
    `/flows/power-platform/environments/${encodeURIComponent(
      environmentId
    )}/flows`,
    {
      headers: {
        "X-PowerPlatform-Access-Token": accessToken,
      },
    }
  );

  return data;
}

export async function importPowerPlatformFlow({
  environmentId,
  workflowId,
  dataverseUrl,
}) {
  const powerPlatformToken =
    await getPowerPlatformAccessToken();

  const dataverseToken =
    await getDataverseAccessToken(dataverseUrl);

  const { data } = await apiClient.post(
    "/flows/import/power-platform",
    {
      environmentId,
      workflowId,
    },
    {
      headers: {
        "X-PowerPlatform-Access-Token":
          powerPlatformToken,

        "X-Dataverse-Access-Token":
          dataverseToken,
      },
    }
  );

  return data;
}