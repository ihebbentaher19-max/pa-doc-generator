import { PublicClientApplication } from "@azure/msal-browser";
import { msalConfig } from "../config/authConfig";

export const msalInstance = new PublicClientApplication(msalConfig);